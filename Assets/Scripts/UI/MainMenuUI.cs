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

    // Optional: animator parameter names used by your settings panel
    [Header("Animator (optional)")]
    [Tooltip("Animator bool used to show/hide the settings panel (if it has an Animator).")]
    [SerializeField] private string settingsShownBool = "Shown";
    [Tooltip("Animator trigger used to show the settings panel (optional).")]
    [SerializeField] private string settingsShowTrigger = "Show";
    [Tooltip("Animator trigger used to hide the settings panel (optional).")]
    [SerializeField] private string settingsHideTrigger = "Hide";

    public void PlayNewGame()
    {
        // Load the Game scene explicitly by name
        SceneManager.LoadScene("Game");
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

        bool settingsIsChildOfMain = mainMenuRoot != null && settingsMenuRoot.transform.IsChildOf(mainMenuRoot.transform);

        settingsMenuRoot.SetActive(true);
        TryDriveAnimator(settingsMenuRoot, true);

        if (!settingsIsChildOfMain && mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }
        else if (settingsIsChildOfMain)
        {
            Debug.Log("MainMenuUI: Settings panel is a child of the assigned main menu root. Keeping parent active.");
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

        TryDriveAnimator(settingsMenuRoot, false);
        settingsMenuRoot.SetActive(false);

        if (!settingsIsChildOfMain && mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }
    }

    // Try to drive a typical animator-based show/hide setup
    private void TryDriveAnimator(GameObject obj, bool show)
    {
        var anim = obj != null ? obj.GetComponent<Animator>() : null;
        if (anim == null) return;

        // Set bool if it exists
        if (!string.IsNullOrEmpty(settingsShownBool))
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == settingsShownBool)
                {
                    anim.SetBool(settingsShownBool, show);
                    break;
                }
            }
        }

        // Fire triggers if they exist
        string trigger = show ? settingsShowTrigger : settingsHideTrigger;
        if (!string.IsNullOrEmpty(trigger))
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
                {
                    anim.ResetTrigger(show ? settingsHideTrigger : settingsShowTrigger);
                    anim.SetTrigger(trigger);
                    break;
                }
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
}
