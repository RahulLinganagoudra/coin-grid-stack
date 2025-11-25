using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player input for drag-to-stack cell selection.
/// Manages the flow: drag start → create stack → add cells → merge → update grid.
/// </summary>
public class InputHandler : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Grid2D grid;

	[Header("Raycast Settings")]
	[SerializeField] private LayerMask cellLayerMask = -1;
	[SerializeField] private float raycastDistance = 100f;

	[Header("Prefabs")]
	[SerializeField] private GameObject stackPrefab;

	// Current drag state
	private bool isDragging = false;
	private Stack currentStack = null;
	private CellData lastHoveredCell = null;
	private Vector2Int currentGridPosition;
	[SerializeField] private float stackMoveSpeed = 20;

	private void Awake()
	{
		if (mainCamera == null)
			mainCamera = Camera.main;

		if (grid == null)
			grid = FindObjectOfType<Grid2D>();

		// Create stack prefab if not assigned
		if (stackPrefab == null)
		{
			stackPrefab = new GameObject("StackPrefab");
			stackPrefab.AddComponent<Stack>();
			stackPrefab.SetActive(false);
		}
	}

	private void Update()
	{
		if (!Grid2D.CanInteract) return;

		HandleInput();
	}

	private void HandleInput()
	{
		// Mouse/Touch input handling
		if (Input.GetMouseButtonDown(0))
		{
			OnDragStart(Input.mousePosition);
		}
		else if (Input.GetMouseButton(0) && isDragging)
		{
			OnDragging(Input.mousePosition);
		}
		else if (Input.GetMouseButtonUp(0) && isDragging)
		{
			OnDragEnd();
		}
	}

	private void OnDragStart(Vector3 screenPosition)
	{
		CellData cell = GetCellAtScreenPosition(screenPosition);

		if (cell != null)
		{
			// Create new stack
			GameObject stackObj = Instantiate(stackPrefab, transform);
			stackObj.SetActive(true);
			currentStack = stackObj.GetComponent<Stack>();

			// Remove cell from grid
			grid.RemoveCell(cell);

			// Initialize stack with first cell
			currentStack.Initialize(cell, stackMoveSpeed);

			isDragging = true;
			lastHoveredCell = cell;
		}
	}

	private void OnDragging(Vector3 screenPosition)
	{
		if (currentStack == null) return;

		// Get desired world position from mouse
		Vector3 desiredWorldPos = GetWorldPositionFromScreen(screenPosition);
		if (desiredWorldPos == Vector3.zero) return;

		// Get ACTUAL current position of the stack and snap it
		Vector3 currentWorldPos = currentStack.transform.position;
		Vector3 currentSnappedPos = grid.SnapWorldPosition(currentWorldPos);

		// Get desired snapped position
		Vector3 desiredSnappedPos = grid.SnapWorldPosition(desiredWorldPos);

		// Calculate delta in grid space (XY plane)
		Vector3 delta = desiredSnappedPos - currentSnappedPos;

		// Determine movement direction based on delta (XY plane)
		Vector2Int moveDirection = Vector2Int.zero;

		// Only process if there's significant movement
		if (Mathf.Abs(delta.x) > 0.01f || Mathf.Abs(delta.y) > 0.01f)
		{
			// Prioritize larger movement axis
			if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
			{
				// Horizontal movement (X axis)
				moveDirection = new Vector2Int(delta.x > 0 ? 1 : -1, 0);
			}
			else
			{
				// Vertical movement (Y axis)
				moveDirection = new Vector2Int(0, delta.y > 0 ? 1 : -1);
			}
		}

		// Get current grid position from ACTUAL current position
		grid.GetGridPosition(currentSnappedPos, out int currentX, out int currentY);
		currentGridPosition = new Vector2Int(currentX, currentY);

		// Check if movement is allowed - DEFAULT to current position (no movement)
		Vector3 allowedWorldPos = currentSnappedPos;

		if (moveDirection != Vector2Int.zero)
		{
			Vector2Int targetGridPos = currentGridPosition + moveDirection;

			// Only allow movement if target position is valid (same color or empty)
			if (CanMoveToPosition(targetGridPos))
			{
				// Allow movement to desired position
				allowedWorldPos = desiredWorldPos;
			}
			// else: stay at currentSnappedPos (blocked)
		}
		else
		{
			// No significant movement direction, allow free movement
			allowedWorldPos = desiredWorldPos;
		}

		// Update stack position
		currentStack.UpdatePosition(allowedWorldPos);

		// Check for cells to add to stack at the allowed position
		// Convert allowed world position to grid coordinates
		grid.GetGridPosition(allowedWorldPos, out int allowedX, out int allowedY);
		CellData cell = grid.GetCellAt(allowedX, allowedY);

		// If hovering over a new cell
		if (cell != null && cell != lastHoveredCell)
		{
			// Check if same color
			if (currentStack.CanAddCell(cell))
			{
				// Remove from grid
				grid.RemoveCell(cell);

				// Add to stack
				currentStack.AddCell(cell);

				lastHoveredCell = cell;
			}
		}
	}

	/// <summary>
	/// Check if the stack can move to the target grid position
	/// </summary>
	private bool CanMoveToPosition(Vector2Int targetPos)
	{
		if (currentStack == null || currentStack.TargetColor == null)
		{
			return false;
		}

		// Check if target position is within grid bounds
		if (targetPos.x < 0 || targetPos.y < 0 || targetPos.x >= grid.GridSize.x || targetPos.y >= grid.GridSize.y)
		{
			return false;
		}

		// Get cell at target position
		CellData targetCell = grid.GetCellAt(targetPos.x, targetPos.y);
		// Allow movement if:
		// 1. Target cell exists and has same color
		// 2. OR target position is empty (allow free movement in empty space)
		if (targetCell != null)
		{
			bool canMove = targetCell.ColorData == currentStack.TargetColor;
			return canMove;
		}

		return true;
	}

	/// <summary>
	/// Get world position from screen position using raycast to a plane (XY plane, Z-forward)
	/// </summary>
	private Vector3 GetWorldPositionFromScreen(Vector3 screenPosition)
	{
		if (mainCamera == null) return Vector3.zero;

		Ray ray = mainCamera.ScreenPointToRay(screenPosition);

		// Create a plane perpendicular to Z axis (for XY plane movement)
		float planeZ = 0f;
		if (currentStack != null)
		{
			planeZ = currentStack.transform.position.z;
		}

		// Plane facing forward (normal = Vector3.forward) at the cell's Z position
		Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, planeZ));

		if (plane.Raycast(ray, out float distance))
		{
			return ray.GetPoint(distance);
		}

		return Vector3.zero;
	}

	private void OnDragEnd()
	{
		isDragging = false;
		lastHoveredCell = null;

		if (currentStack == null) return;

		// Only merge if we have at least 2 cells
		if (currentStack.Count >= 2)
		{
			// Play merge animation
			currentStack.PlayMergeAnimation(() =>
			{
				// After merge, update grid positions
				grid.UpdatePositions();
			});
		}
		else
		{
			// Cancel - return cell to grid
			if (currentStack.Count == 1)
			{
				CellData cell = currentStack.Cells[0];
				Vector2Int gridPos = cell.GridPosition;
				grid.AddCell(cell, gridPos.x, gridPos.y);
				cell.GetComponent<Collider>().enabled = true;
			}

			// Destroy stack
			Destroy(currentStack.gameObject);
		}

		currentStack = null;
	}

	/// <summary>
	/// Raycast to get the cell at the given screen position
	/// </summary>
	private CellData GetCellAtScreenPosition(Vector3 screenPosition)
	{
		if (mainCamera == null) return null;

		Ray ray = mainCamera.ScreenPointToRay(screenPosition);
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, raycastDistance, cellLayerMask))
		{
			// Try to get CellData component from hit object or its parents
			CellData cell = hit.collider.GetComponent<CellData>();
			if (cell == null)
				cell = hit.collider.GetComponentInParent<CellData>();

			return cell;
		}

		return null;
	}

	/// <summary>
	/// Cancel current drag operation
	/// </summary>
	public void CancelDrag()
	{
		if (isDragging && currentStack != null)
		{
			// Return all cells to grid
			foreach (var cell in currentStack.Cells)
			{
				if (cell != null)
				{
					Vector2Int gridPos = cell.GridPosition;
					grid.AddCell(cell, gridPos.x, gridPos.y);
					cell.GetComponent<Collider>().enabled = true;
				}
			}

			Destroy(currentStack.gameObject);
			currentStack = null;
		}

		isDragging = false;
		lastHoveredCell = null;
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (isDragging && currentStack != null && currentStack.Count > 0)
		{
			// Draw debug sphere at stack position
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(currentStack.transform.position, 0.3f);

			// Draw stack count
			UnityEditor.Handles.Label(
				currentStack.transform.position + Vector3.up * 0.5f,
				$"Stack: {currentStack.Count}"
			);
		}
	}
#endif
}
