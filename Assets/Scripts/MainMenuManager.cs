using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
	[Header("Settings")]
	public string gameSceneName = "SampleScene";

	public void PlayGame()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(gameSceneName);
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void LoadSceneByIndex(int index)
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(index);
	}
}