using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Panels (do NOT assign the Canvas root here)")]
    [Tooltip("The main menu panel GameObject (e.g., 'Main Window').")]
    [SerializeField] private GameObject mainMenuRoot;
    [Tooltip("The settings panel GameObject (e.g., 'Settings Menu').")]
    [SerializeField] private GameObject settingsMenuRoot;


    public void PlayNewGame()
    {
        // Load the Game scene explicitly by name
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        // TODO: load save; for now just start game
        PlayNewGame();
    }

    // Called by the Settings button in the Main Menu
    public void OpenSettings()
    {
        if (settingsMenuRoot == null)
        {
            Debug.LogWarning("MainMenuUI: 'settingsMenuRoot' not assigned.");
            return;
        }
    }

    // Called by a Back/Close button in the Settings Menu
    public void CloseSettings()
    {
        if (settingsMenuRoot == null)
        {
            Debug.LogWarning("MainMenuUI: 'settingsMenuRoot' not assigned.");
            return;
        }

        bool settingsIsChildOfMain = mainMenuRoot != null && settingsMenuRoot.transform.IsChildOf(mainMenuRoot.transform);

        settingsMenuRoot.SetActive(false);

        if (!settingsIsChildOfMain && mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }
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
