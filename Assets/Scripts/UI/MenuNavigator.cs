using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuNavigator : MonoBehaviour
{
    [Header("Assign your menu roots (top-level GameObjects)")]
    public GameObject mainMenuRoot;
    public GameObject settingsMenuRoot;

    [Header("Gameplay Scene")]
    [SerializeField] private string gameSceneName = "Game";

    public void ShowMainMenu()
    {
        if (mainMenuRoot) mainMenuRoot.SetActive(true);
        if (settingsMenuRoot) settingsMenuRoot.SetActive(false);
    }

    public void ShowSettings()
    {
        if (mainMenuRoot) mainMenuRoot.SetActive(false);
        if (settingsMenuRoot) settingsMenuRoot.SetActive(true);
    }

    public void PlayNewGame()
    {
        SaveManager.ClearQueuedLoad();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.SaveExists())
        {
            SaveManager.QueueLoadOnNextScene();
        }
        else
        {
            PlayNewGame();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}