using UnityEngine;

public class CameraCopySettings : MonoBehaviour
{
	[SerializeField] Camera overlayCamera;

	[ContextMenu("UpdateCamera")]
	private void OnValidate()
	{
		if (overlayCamera == null) return;
		Camera mainCam = Camera.main;
		overlayCamera.transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);
		overlayCamera.transform.localScale = mainCam.transform.localScale;

		overlayCamera.fieldOfView = mainCam.fieldOfView;
	}
}
