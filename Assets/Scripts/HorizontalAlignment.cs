using UnityEngine;

public class HorizontalAlignment : MonoBehaviour
{
	private Vector3 slotsHalfLength;
	[SerializeField] Vector3 offset = Vector3.right;
	[SerializeField] Vector3 eulerRotation = Vector3.zero;
	[SerializeField] private float spacing;

	// Start is called before the first frame update
	void Start()
	{
		Align();
	}
#if UNITY_EDITOR
	private void OnValidate()
	{
		slotsHalfLength = (transform.childCount - 1) * spacing * offset / 2f;
		Align();
	}
	private void OnDrawGizmos()
	{
		slotsHalfLength = (transform.childCount - 1) * spacing * offset / 2f;
		for (int i = 0; i < 5; i++)
		{
			Gizmos.DrawSphere(GetSlotPosition(i), .2f);
		}
	}
	[ContextMenu("Align")]
#endif
	public void Align()
	{
		slotsHalfLength = (transform.childCount - 1) * spacing * offset / 2f;
		for (int i = 0; i < transform.childCount; i++)
		{
			var child = transform.GetChild(i);
			child.position = GetSlotPosition(i);
			child.eulerAngles = eulerRotation;
		}
	}
	public Vector3 GetSlotPosition(int slotIndex)
	{
		return (transform.position + slotIndex * spacing * offset) - slotsHalfLength;
	}
}
