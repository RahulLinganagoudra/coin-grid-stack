using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Game/Color Data")]
public class ColorData : ScriptableObject
{
	[SerializeField] private Material cubeMaterial;

	public Material CubeMaterial { get => cubeMaterial;}
}
