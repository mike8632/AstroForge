using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuNavigator : MonoBehaviour
{
    [Header("Panels (do NOT assign the Canvas root)")]
    [Tooltip("Main menu panel root (e.g., 'Main Menu').")]
    [SerializeField] private GameObject mainMenuRoot;

    [Tooltip("Settings panel root (e.g., 'Settings Menu').")]
    [SerializeField] private GameObject settingsMenuRoot;

    [Tooltip("Optional loading panel root to show while scenes load.")]
    [SerializeField] private GameObject loadingPanelRoot;

    [Header("Scene Loading")]
    [SerializeField] private string gameSceneName = "Game";
    [Tooltip("Use SceneManager.LoadSceneAsync instead of LoadScene.")]
    [SerializeField] private bool loadAsync = true;

    [Tooltip("If true, Main Menu will be hidden when Settings is shown (only matters when Settings isn't a child of Main).")]
    [SerializeField] private bool hideMainWhileInSettings = true;

    [Header("Continue Game")]
    [Tooltip("Assign your Continue button so it can be auto-disabled when no save is present.")]
    [SerializeField] private Button continueButton;

    [Tooltip("PlayerPrefs key used to detect whether a save exists.")]
    [SerializeField] private string continueFlagKey = "save_exists";

#if UNITY_EDITOR
    [Tooltip("Editor-only: force Continue to be enabled while testing, even if no save flag is set.")]
    [SerializeField] private bool simulateSaveInEditor = false;
#endif

    [Tooltip("If no save exists, Continue will either do nothing (false) or start a new game (true).")]
    [SerializeField] private bool continueFallsBackToNewGame = true;

    private void Awake()
    {
        UpdateContinueButton();
    }

    private void OnEnable()
    {
        UpdateContinueButton();
    }

    // -------- Navigation between panels --------

    public void ShowMainMenu()
    {
        if (mainMenuRoot) mainMenuRoot.SetActive(true);
        if (settingsMenuRoot) settingsMenuRoot.SetActive(false);
    }

    public void ShowSettings()
    {
        if (!settingsMenuRoot)
        {
            Debug.LogWarning("MenuNavigator: 'settingsMenuRoot' not assigned.");
            return;
        }

        settingsMenuRoot.SetActive(true);

        // Hide main only if settings isn't already visually inside main
        if (mainMenuRoot && hideMainWhileInSettings && !settingsMenuRoot.transform.IsChildOf(mainMenuRoot.transform))
        {
            mainMenuRoot.SetActive(false);
        }
    }

    public void CloseSettings()
    {
        if (!settingsMenuRoot)
        {
            Debug.LogWarning("MenuNavigator: 'settingsMenuRoot' not assigned.");
            return;
        }

        bool settingsIsChildOfMain = mainMenuRoot != null && settingsMenuRoot.transform.IsChildOf(mainMenuRoot.transform);

        settingsMenuRoot.SetActive(false);

        // If settings lives outside main, bring main back when closing settings
        if (!settingsIsChildOfMain && mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }
    }

    // -------- Game flow --------

    public void PlayNewGame()
    {
        // If you want a "fresh start", clear/initialize your save here.
        // Example: PlayerPrefs.DeleteKey(continueFlagKey);

        // Ensure the game is unpaused before loading
        Time.timeScale = 1f;
        LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        if (HasSave())
        {
            Time.timeScale = 1f;
            LoadScene(gameSceneName);
        }
        else
        {
            if (continueFallsBackToNewGame)
            {
                PlayNewGame();
            }
            else
            {
                Debug.Log("MenuNavigator: No save found. Continue is disabled.");
            }
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

    // -------- Helpers --------

    public void SetHasSave(bool has)
    {
        PlayerPrefs.SetInt(continueFlagKey, has ? 1 : 0);
        PlayerPrefs.Save();
        UpdateContinueButton();
    }

    private bool HasSave()
    {
        bool has = PlayerPrefs.GetInt(continueFlagKey, 0) == 1;
#if UNITY_EDITOR
        if (simulateSaveInEditor) has = true;
#endif
        return has;
    }

    private void UpdateContinueButton()
    {
        if (continueButton)
            continueButton.interactable = HasSave();
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("MenuNavigator: gameSceneName is empty. Set it in the Inspector.");
            return;
        }

        if (loadAsync)
            StartCoroutine(LoadSceneRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (loadingPanelRoot) loadingPanelRoot.SetActive(true);

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        if (loadingPanelRoot) loadingPanelRoot.SetActive(false);
    }

    // Optional: quick menu hooks if you wire multiple scenes
    public void LoadSceneByName(string sceneName) => LoadScene(sceneName);
}