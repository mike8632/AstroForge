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

    private bool isLoading;

    public void SaveAndReturnToMainMenu()
    {
        if (isLoading) return;

        try
        {

            if (OnSaveRequested != null)
            {
                OnSaveRequested.Invoke();
            }
            else
            {
                SaveManager.Instance?.SaveGame();
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"Save threw: {e.Message}"); }

        PlayerPrefs.SetInt(continueFlagKey, 1);
        PlayerPrefs.Save();

        if (Time.timeScale == 0f) Time.timeScale = 1f;

        SaveManager.ClearQueuedLoad();

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("ReturnToMainWithSave: mainMenuSceneName is empty.");
            return;
        }

        isLoading = true;

        if (loadAsync)
        {
            StopAllCoroutines();
            StartCoroutine(LoadMenuAsync(mainMenuSceneName));
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private IEnumerator LoadMenuAsync(string sceneName)
    {
        if (loadingPanelRoot) loadingPanelRoot.SetActive(true);

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        if (loadingPanelRoot) loadingPanelRoot.SetActive(false);
    }
}