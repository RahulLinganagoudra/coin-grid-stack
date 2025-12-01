using DT.GridSystem;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityUtils;

/// <summary>
/// 2D Grid system for managing cells with drop mechanics.
/// Replicates BlastGrid's drop logic for cell positioning.
/// </summary>
public class Grid2D : GridSystem2D<CellData>
{
	[Header("Drop Settings")]
	[SerializeField] private float dropDuration = 0.5f;
	[SerializeField] private Ease dropEase = Ease.OutBack;
	[SerializeField] Vector2 CellSizeO = new Vector2(0.8f, 1f);
	public static bool CanInteract = true;


	protected override void Awake()
	{
	}

	public override Vector3 GetWorldPosition(int x, int y, bool snapToGrid = true)
	{
		Vector2 halfGrid = new Vector2(GridSize.x / 2f, GridSize.y / 2f);

		// Base bottom-left calculation
		Vector3 worldPos = new Vector3(
			(x - halfGrid.x) * CellSizeO.x,
			(y - halfGrid.y) * CellSizeO.y,
			0f
		) + transform.position;

		if (!snapToGrid)
			return worldPos;

		// Add half cell offset for center
		return worldPos + new Vector3(CellSizeO.x * 0.5f, CellSizeO.y * 0.5f, 0f);
	}

	public override void GetGridPosition(Vector3 worldPosition, out int x, out int y)
	{
		// Convert world  local grid coordinates
		float relativeX = (worldPosition.x - transform.position.x) / CellSizeO.x;
		float relativeY = (worldPosition.y - transform.position.y) / CellSizeO.y;

		x = Mathf.FloorToInt(relativeX + GridSize.x / 2f);
		y = Mathf.FloorToInt(relativeY + GridSize.y / 2f);

		// Clamp safely
		x = Mathf.Clamp(x, 0, GridSize.x - 1);
		y = Mathf.Clamp(y, 0, GridSize.y - 1);
	}

	/// <summary>
	/// Get cell at grid position
	/// </summary>
	public CellData GetCellAt(int x, int y)
	{
		if (TryGetGridObject(x, y, out var cell))
			return cell;
		return null;
	}

	/// <summary>
	/// Remove cell from grid at position
	/// </summary>
	public void RemoveCell(int x, int y)
	{
		if (x < 0 || y < 0 || x >= GridSize.x || y >= GridSize.y)
			return;

		RemoveGridObject(x, y);
	}

	/// <summary>
	/// Remove specific cell from grid
	/// </summary>
	public void RemoveCell(CellData cell)
	{
		if (cell == null) return;
		cell.GetComponent<Collider>().enabled = false;
		RemoveCell(cell.GridPosition.x, cell.GridPosition.y);
	}

	/// <summary>
	/// Add cell to grid at position
	/// </summary>
	public void AddCell(CellData cell, int x, int y)
	{
		if (cell == null) return;
		if (x < 0 || y < 0 || x >= GridSize.x || y >= GridSize.y)
			return;

		cell.GridPosition = new Vector2Int(x, y);
		AddGridObject(x, y, cell);
	}

	/// <summary>
	/// Update all cell positions with drop mechanics
	/// </summary>
	public void UpdatePositions()
	{
		CanInteract = false;

		// Collect all affected columns
		HashSet<int> affectedColumns = new HashSet<int>();
		for (int x = 0; x < GridSize.x; x++)
		{
			affectedColumns.Add(x);
		}

		DropAffectedColumns(affectedColumns);
	}

	/// <summary>
	/// Drop cells in affected columns (replicates BlastGrid logic)
	/// </summary>
	private void DropAffectedColumns(HashSet<int> columns)
	{
		// Collect all cells that need to be repositioned
		List<CellData> cellsToReposition = new List<CellData>();

		foreach (int col in columns)
		{
			if (col < 0 || col >= GridSize.x) continue;

			for (int y = 0; y < GridSize.y; y++)
			{
				if (TryGetGridObject(col, y, out var cell) && cell != null)
				{
					cellsToReposition.Add(cell);
					RemoveGridObject(col, y);
				}
			}
		}

		// Sort by Y position (bottom to top)
		cellsToReposition.Sort((a, b) => a.GridPosition.y.CompareTo(b.GridPosition.y));

		// Re-add cells from bottom up
		foreach (var cell in cellsToReposition)
		{
			int x = cell.GridPosition.x;
			int targetY = FindLowestEmptyPosition(x);

			if (targetY >= 0)
			{
				// Update grid position
				cell.GridPosition = new Vector2Int(x, targetY);
				AddGridObject(x, targetY, cell);

				// Animate to new position
				Vector3 targetWorldPos = GetWorldPosition(x, targetY);
				cell.AnimateToPosition(targetWorldPos, dropDuration);
			}
		}

		// Spawn new cells to fill empty spaces at the top
		SpawnNewCellsForEmptySpaces(columns);

		// Re-enable interaction after animation
		DOVirtual.DelayedCall(dropDuration, () =>
		{
			CanInteract = true;
		});
	}

	/// <summary>
	/// Find the lowest empty position in a column
	/// </summary>
	private int FindLowestEmptyPosition(int x)
	{
		for (int y = 0; y < GridSize.y; y++)
		{
			if (!TryGetGridObject(x, y, out var cell) || cell == null)
				return y;
		}
		return -1; // Column is full
	}

	/// <summary>
	/// Spawn new cells to fill empty spaces in affected columns
	/// </summary>
	private void SpawnNewCellsForEmptySpaces(HashSet<int> columns)
	{
		if (cellPrefab == null || availableColors == null || availableColors.Length == 0)
		{
			Debug.LogWarning("Cannot spawn new cells: cellPrefab or availableColors not set");
			return;
		}

		foreach (int col in columns)
		{
			if (col < 0 || col >= GridSize.x) continue;

			// Find all empty positions in this column from top to bottom
			for (int y = GridSize.y - 1; y >= 0; y--)
			{
				if (!TryGetGridObject(col, y, out var cell) || cell == null)
				{
					// Spawn new cell at this position
					SpawnNewCell(col, y);
				}
			}
		}
	}

	/// <summary>
	/// Spawn a new cell at the specified grid position with random color
	/// </summary>
	private void SpawnNewCell(int x, int y)
	{
		// Get target world position
		Vector3 targetWorldPos = GetWorldPosition(x, y);

		// Spawn position above the grid
		Vector3 spawnWorldPos = GetWorldPosition(x, GridSize.y);

		// Instantiate cell
		GameObject cellObj = Instantiate(cellPrefab, spawnWorldPos, Quaternion.identity, transform);
		cellObj.name = $"Cell_{x}_{y}";

		// Get or add CellData component
		CellData cellData = cellObj.GetComponent<CellData>();
		if (cellData == null)
			cellData = cellObj.AddComponent<CellData>();

		// Assign random color
		ColorData randomColor = availableColors[Random.Range(0, availableColors.Length)];
		cellData.Initialize(randomColor, new Vector2Int(x, y));

		// Add to grid
		AddGridObject(x, y, cellData);

		// Animate to target position
		cellData.AnimateToPosition(targetWorldPos, dropDuration);
	}
	[Header("Editor Settings")]
	[SerializeField] private GameObject cellPrefab;
	[SerializeField] private ColorData[] availableColors;

	[EditorButton]
	public void CreateGrid()
	{
		// Clear existing grid first
		ClearGrid();

		if (cellPrefab == null)
		{
			Debug.LogError("Cell prefab is not assigned!");
			return;
		}

		// Create cells for each grid position
		for (int x = 0; x < GridSize.x; x++)
		{
			for (int y = 0; y < GridSize.y; y++)
			{
				// Instantiate cell
				Vector3 worldPos = GetWorldPosition(x, y);
				GameObject cellObj = UnityEditor.PrefabUtility.InstantiatePrefab(cellPrefab, transform) as GameObject;
				cellObj.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
				cellObj.name = $"Cell_{x}_{y}";

				// Get or add CellData component
				CellData cellData = cellObj.GetOrAdd<CellData>();

				// Assign random color if available
				if (availableColors != null && availableColors.Length > 0)
				{
					ColorData randomColor = availableColors[Random.Range(0, availableColors.Length)];
					cellData.Initialize(randomColor, new Vector2Int(x, y));
				}
				else
				{
					cellData.GridPosition = new Vector2Int(x, y);
				}

				// Add to grid
				AddGridObject(x, y, cellData);
			}
		}
	}

	[EditorButton]
	public void ClearGrid()
	{
		// Destroy all cell GameObjects
		for (int x = 0; x < GridSize.x; x++)
		{
			for (int y = 0; y < GridSize.y; y++)
			{
				if (TryGetGridObject(x, y, out var cell) && cell != null)
				{
					if (Application.isPlaying)
						Destroy(cell.gameObject);
					else
						DestroyImmediate(cell.gameObject);

					RemoveGridObject(x, y);
				}
			}
		}

		Debug.Log("Grid cleared");
	}

	[EditorButton]
	public void RandomizeColorsGrid()
	{
		if (availableColors == null || availableColors.Length == 0)
		{
			Debug.LogError("No colors available! Assign colors in the availableColors array.");
			return;
		}

		int colorizedCount = 0;

		// Randomize colors for all existing cells
		for (int x = 0; x < GridSize.x; x++)
		{
			for (int y = 0; y < GridSize.y; y++)
			{
				if (TryGetGridObject(x, y, out var cell) && cell != null)
				{
					ColorData randomColor = availableColors[Random.Range(0, availableColors.Length)];
					cell.SetColorData(randomColor);
					colorizedCount++;
				}
			}
		}
		UnityEditor.EditorUtility.SetDirty(this);

		Debug.Log($"Randomized colors for {colorizedCount} cells");
	}
}

