using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseSettingsController : MonoBehaviour
{
    [Header("Assign the Settings panel root in your Game scene")]
    [SerializeField] private GameObject settingsMenuRoot;

    [Header("Behavior")]
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.Escape; // used only if old Input is active
    [SerializeField] private bool pauseGame = true;      // Time.timeScale = 0 while open
    [SerializeField] private bool pauseAudio = false;    // Optional: AudioListener.pause while open
    [SerializeField] private bool unlockCursorOnOpen = true;

    [Header("Optional: UI focus when opening")]
    [SerializeField] private GameObject firstSelected;   // e.g., a "Resume" button or your Resolution dropdown

    private float _prevTimeScale = 1f;
    private bool _prevAudioPaused = false;
    private bool _wasCursorVisible = false;
    private CursorLockMode _wasCursorLock;

#if ENABLE_INPUT_SYSTEM
    private InputAction _toggleAction;
#endif

    private void Start()
    {
        if (settingsMenuRoot)
            settingsMenuRoot.SetActive(false);
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (_toggleAction == null)
         {
            _toggleAction = new InputAction("PauseToggle");
            // Keyboard Escape
            _toggleAction.AddBinding("<Keyboard>/escape");
            // Gamepad Start/Options buttons
            _toggleAction.AddBinding("<Gamepad>/start");
            _toggleAction.AddBinding("<Gamepad>/select");
        }

        _toggleAction.performed += OnTogglePerformed;
        _toggleAction.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (_toggleAction != null)
        {
            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Disable();
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnTogglePerformed(InputAction.CallbackContext ctx) => Toggle();
#else
    private void Update()
    {
        // Old Input API path (only compiled/used when new Input System is not enabled)
        if (Input.GetKeyDown(legacyToggleKey))
            Toggle();
    }
#endif

    public void Toggle()
    {
        if (!settingsMenuRoot) return;

        if (settingsMenuRoot.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (!settingsMenuRoot || settingsMenuRoot.activeSelf) return;

        if (pauseGame)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (pauseAudio)
        {
            _prevAudioPaused = AudioListener.pause;
            AudioListener.pause = true;
        }

        if (unlockCursorOnOpen)
        {
            _wasCursorLock = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        settingsMenuRoot.SetActive(true);

        var es = EventSystem.current;
        if (es)
        {
            GameObject target = firstSelected;
            if (!target)
            {
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

    public void Resume() => Close();
}