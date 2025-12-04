using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Represents a stack of dragged cells.
/// Manages cell collection, visual stacking, and merge animation.
/// Cells are parented to this transform and use local positions.
/// </summary>
public class Stack : MonoBehaviour
{
	[Header("Stack Settings")]
	[SerializeField] private Vector3 stackOffset = new(0, 0.1f, -0.1f);
	[SerializeField] private float mergeAnimationDelay = 0.03f;
	[SerializeField] private float cellAddDuration = 0.3f;

	private List<CellData> cells = new List<CellData>();
	private ColorData targetColor = null;
	private float moveSpeed;

	public int Count => cells.Count;
	public ColorData TargetColor => targetColor;
	public List<CellData> Cells => cells;

	/// <summary>
	/// Initialize the stack with the first cell
	/// </summary>
	public void Initialize(CellData firstCell, float moveSpeed)
	{
		if (firstCell == null) return;

		cells.Clear();
		targetColor = firstCell.ColorData;

		// Position the stack at the first cell's position
		transform.position = firstCell.transform.position;
		this.moveSpeed = moveSpeed;
		AddCell(firstCell);
	}

	/// <summary>
	/// Add a cell to the stack
	/// </summary>
	public void AddCell(CellData cell)
	{
		if (cell == null) return;

		// Validate same color
		if (targetColor != null && cell.ColorData != targetColor)
		{
			Debug.LogWarning($"Cannot add cell with different color to stack. Expected {targetColor.name}, got {cell.ColorData.name}");
			return;
		}

		cells.Add(cell);

		// Parent the cell to this stack transform
		cell.transform.SetParent(transform);

		// Calculate target local position for this cell (stack top)
		Vector3 targetLocalPosition = stackOffset * (cells.Count - 1);

		// Animate cell to its position in the stack using DOTween
		cell.transform.DOKill();
		cell.transform.DOLocalMove(targetLocalPosition, cellAddDuration)
			.SetEase(Ease.OutBack);
	}

	/// <summary>
	/// Update the stack's world position (cells follow automatically as children)
	/// </summary>
	public void UpdatePosition(Vector3 newPosition)
	{
		// Smoothly move the entire stack transform
		transform.position = Vector3.Lerp(transform.position, newPosition, moveSpeed * Time.deltaTime);
	}

	/// <summary>
	/// Check if a cell can be added to this stack
	/// </summary>
	public bool CanAddCell(CellData cell)
	{
		if (cell == null) return false;
		if (targetColor == null) return true;
		return cell.ColorData == targetColor;
	}

	/// <summary>
	/// Play merge animation for all cells in stack
	/// </summary>
	public void PlayMergeAnimation(Action onComplete)
	{
		if (cells.Count == 0)
		{
			onComplete?.Invoke();
			Destroy(gameObject);
			return;
		}

		StartCoroutine(PlayMergeAnimationRoutine());

		IEnumerator PlayMergeAnimationRoutine()
		{
			for (int i = cells.Count - 1; i >= 0; i--)
			{
				cells[i].ColorizeSprites();
			}
			yield return new WaitForSeconds(.5f);

			float delayBetweenCells = 0.081f; // Customize this delay as desired
			Sequence mergeSequence = DOTween.Sequence();

			for (int i = cells.Count - 1; i >= 0; i--)
			{
				CellData cell = cells[i];
				if (cell != null)
				{
					cell.transform.DOKill();
					Sequence cellSequence = DOTween.Sequence();

					cellSequence.Join(
						cell.transform.DOScale(Vector3.zero, 0.5f)
						.SetEase(Ease.InBack)
						.OnComplete(() => cell.gameObject.SetActive(false))
					);

					cellSequence.Join(cell.transform.DOLocalRotate(new Vector3(0, 0, -45), 0.3f));

					// Corrected insert timing for immediate start
					mergeSequence.Insert((cells.Count - 1 - i) * delayBetweenCells, cellSequence);
				}
			}

			mergeSequence.OnComplete(() =>
			{
				onComplete?.Invoke();
				Destroy(gameObject);
			});

			yield return mergeSequence.WaitForCompletion();
		}

	}

	/// <summary>
	/// Clear the stack without animation
	/// </summary>
	public void Clear()
	{
		cells.Clear();
		targetColor = null;
	}
}
