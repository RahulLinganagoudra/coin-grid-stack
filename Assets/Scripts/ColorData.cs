using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Game/Color Data")]
public class ColorData : ScriptableObject
{
	[SerializeField] private Material cubeMaterial;
	[SerializeField] private Material coloredTiles;


	public Material CubeMaterial { get => cubeMaterial; }
	public Material ColoredMat { get => coloredTiles; }
}
