using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ReturnToMainWithSave : MonoBehaviour
{
    [Header("Scene to load")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Optional loading panel (shown during async load)")]
    [SerializeField] private GameObject loadingPanelRoot;
    [SerializeField] private bool loadAsync = false;

    [Header("Save hook (call your save code here)")]
    public UnityEvent OnSaveRequested;

    [Header("Continue flag (so Continue button enables)")]
    [SerializeField] private string continueFlagKey = "save_exists";

    // Call this from your Pause menu "Main Menu" button
    public void SaveAndReturnToMainMenu()
    {
        // 1) Run your save code (if wired)
        try { OnSaveRequested?.Invoke(); }
        catch (System.Exception e) { Debug.LogWarning($"Save threw: {e.Message}"); }

        // 2) Mark that a save exists (for your Continue button)
        PlayerPrefs.SetInt(continueFlagKey, 1);
        PlayerPrefs.Save();

        // 3) Ensure game is unpaused before scene switch
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // 4) Go to main menu
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("ReturnToMainWithSave: mainMenuSceneName is empty.");
            return;
        }

        if (loadAsync)
            StartCoroutine(LoadMenuAsync(mainMenuSceneName));
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator LoadMenuAsync(string sceneName)
    {
        if (loadingPanelRoot) loadingPanelRoot.SetActive(true);

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        if (loadingPanelRoot) loadingPanelRoot.SetActive(false);
    }
}