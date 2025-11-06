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

    // Start/New Game
    public void PlayNewGame()
    {
        // Ensure we DO NOT auto-load a save when starting fresh
        SaveManager.ClearQueuedLoad();
        // Optional: wipe existing save so Continue disables next boot
        // SaveManager.Instance?.DeleteSave();

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // Continue Game
    public void ContinueGame()
    {
        // Only queue a load when we actually want to restore state
        if (SaveManager.Instance != null && SaveManager.Instance.SaveExists())
        {
            SaveManager.QueueLoadOnNextScene();
        }
        else
        {
            // No save exists -> start a new game instead (or disable the Continue button in UI)
            PlayNewGame();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // Quit
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}