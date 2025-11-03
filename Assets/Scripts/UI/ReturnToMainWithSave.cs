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

    public void SaveAndReturnToMainMenu()
    {
        try
        {
            if (OnSaveRequested != null && OnSaveRequested.GetPersistentEventCount() > 0)
            {
                OnSaveRequested.Invoke();
            }
            else
            {
                var sm = SaveManager.Instance;
                if (sm != null) sm.SaveGame();
                else Debug.LogWarning("ReturnToMainWithSave: No SaveManager.Instance present to save.");
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"Save threw: {e.Message}"); }

        // 2) Mark that a save exists (for Continue)
        PlayerPrefs.SetInt(continueFlagKey, 1);
        PlayerPrefs.Save();

        // 3) Unpause
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // 4) Load main menu
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