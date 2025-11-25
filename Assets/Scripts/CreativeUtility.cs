using UnityEngine;

public class CreativeUtility : MonoBehaviour
{
	[SerializeField] KeyCode reloadKey = KeyCode.R;
	[SerializeField] KeyCode pauseKey = KeyCode.P;
	private void Update()
	{
		if (Input.GetKeyDown(reloadKey))
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
		}
		if (Input.GetKeyDown(pauseKey))
		{
			UnityEditor.EditorApplication.isPaused = !UnityEditor.EditorApplication.isPaused;
		}
	}
}
