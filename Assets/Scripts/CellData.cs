using UnityEngine;
using DG.Tweening;

/// <summary>
/// MonoBehaviour component for individual grid cells.
/// Stores cell data including color, position, and visual references.
/// </summary>
public class CellData : MonoBehaviour
{
	[Header("Cell Properties")]
	[SerializeField] private ColorData colorData;
	[SerializeField] private Vector2Int gridPosition;

	[Header("Visual References")]
	[SerializeField] private MeshRenderer meshRenderer;

	public ColorData ColorData => colorData;
	public Vector2Int GridPosition { get => gridPosition; set => gridPosition = value; }
	public MeshRenderer MeshRenderer => meshRenderer;

	private void Awake()
	{
		if (meshRenderer == null)
			meshRenderer = GetComponentInChildren<MeshRenderer>();
	}

	/// <summary>
	/// Initialize the cell with color data and grid position
	/// </summary>
	public void Initialize(ColorData color, Vector2Int position)
	{
		colorData = color;
		gridPosition = position;

		if (colorData != null && meshRenderer != null)
		{
			meshRenderer.material = colorData.CubeMaterial;
		}
		UnityEditor.EditorUtility.SetDirty(this);
	}

	/// <summary>
	/// Set the color data and update material
	/// </summary>
	public void SetColorData(ColorData color)
	{
		colorData = color;
		if (colorData != null && meshRenderer != null)
		{
			meshRenderer.sharedMaterial = colorData.CubeMaterial;
		}
		UnityEditor.EditorUtility.SetDirty(this);
	}
	public void ColorizeSprites()
	{
		if (colorData != null && meshRenderer != null)
		{
			meshRenderer.sharedMaterial = colorData.ColoredMat;
		}
	}

	/// <summary>
	/// Animate to a new position
	/// </summary>
	public void AnimateToPosition(Vector3 targetPosition, float duration, System.Action onComplete = null)
	{
		transform.DOKill();
		transform.DOMove(targetPosition, duration)
			.SetEase(Ease.OutBack)
			.OnComplete(() => onComplete?.Invoke());
	}

	private void OnDestroy()
	{
		transform.DOKill();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (meshRenderer == null)
			meshRenderer = GetComponentInChildren<MeshRenderer>();
		if (colorData == null)
			return;
		if (colorData != null && meshRenderer != null)
		{
			meshRenderer.sharedMaterial = colorData.CubeMaterial;
		}
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif
}
