using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseSettingsController : MonoBehaviour
{
    [Header("Assign the Settings panel root in your Game scene")]
    [SerializeField] private GameObject settingsMenuRoot;

    [Header("Behavior")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] private bool pauseGame = true;      // Time.timeScale = 0 while open
    [SerializeField] private bool pauseAudio = false;    // Optional: AudioListener.pause while open
    [SerializeField] private bool unlockCursorOnOpen = true;

    [Header("Optional: UI focus when opening")]
    [SerializeField] private GameObject firstSelected;   // e.g., your ResolutionDropdown or a "Resume" button

    private float _prevTimeScale = 1f;
    private bool _prevAudioPaused = false;
    private bool _wasCursorVisible = false;
    private CursorLockMode _wasCursorLock;

    private void Start()
    {
        if (settingsMenuRoot)
            settingsMenuRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        if (!settingsMenuRoot) return;

        if (settingsMenuRoot.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (!settingsMenuRoot || settingsMenuRoot.activeSelf) return;

        // Pause gameplay
        if (pauseGame)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // Pause audio if desired
        if (pauseAudio)
        {
            _prevAudioPaused = AudioListener.pause;
            AudioListener.pause = true;
        }

        // Cursor state
        if (unlockCursorOnOpen)
        {
            _wasCursorLock = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        settingsMenuRoot.SetActive(true);

        // Set UI focus
        var es = EventSystem.current;
        if (es)
        {
            GameObject target = firstSelected;

            if (!target)
            {
                // Try to find any Selectable under the settings menu
                var selectable = settingsMenuRoot.GetComponentInChildren<Selectable>(true);
                if (selectable) target = selectable.gameObject;
            }

            if (target)
            {
                es.SetSelectedGameObject(null);
                es.SetSelectedGameObject(target);
            }
        }
    }

    public void Close()
    {
        if (!settingsMenuRoot || !settingsMenuRoot.activeSelf) return;

        settingsMenuRoot.SetActive(false);

        // Restore gameplay
        if (pauseGame)
            Time.timeScale = _prevTimeScale;

        if (pauseAudio)
            AudioListener.pause = _prevAudioPaused;

        if (unlockCursorOnOpen)
        {
            Cursor.lockState = _wasCursorLock;
            Cursor.visible = _wasCursorVisible;
        }
    }

    // Optional: hook a "Resume" or "Go Back" button to this
    public void Resume() => Close();
}