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
    [SerializeField] private KeyCode legacyToggleKey = KeyCode.Escape;
    [SerializeField] private bool pauseGame = true;
    [SerializeField] private bool pauseAudio = false;
    [SerializeField] private bool unlockCursorOnOpen = true;

    [Header("Optional: UI focus when opening")]
    [SerializeField] private GameObject firstSelected;

    [Header("Sub-menus inside the pause menu")]
    [SerializeField] private GameObject[] subMenusToCloseFirst;

    [Header("Keep game paused while these are active")]
    [SerializeField] private GameObject[] pauseKeepAliveWhileActive;

    private float _prevTimeScale = 1f;
    private bool _pauseApplied = false;

    private bool _prevAudioPaused = false;
    private bool _audioApplied = false;

    private bool _wasCursorVisible = false;
    private CursorLockMode _wasCursorLock;
    private bool _cursorApplied = false;

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
            _toggleAction.AddBinding("<Keyboard>/escape");
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
        if (Input.GetKeyDown(legacyToggleKey))
            Toggle();
    }
#endif

    public void Toggle()
    {
        if (!settingsMenuRoot) return;

        if (CloseAnyActiveSubMenu())
            return;

        if (settingsMenuRoot.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (!settingsMenuRoot || settingsMenuRoot.activeSelf) return;

        if (pauseGame && !_pauseApplied)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _pauseApplied = true;
        }

        if (pauseAudio && !_audioApplied)
        {
            _prevAudioPaused = AudioListener.pause;
            AudioListener.pause = true;
            _audioApplied = true;
        }

        if (unlockCursorOnOpen && !_cursorApplied)
        {
            _wasCursorLock = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorApplied = true;
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

        bool keepPaused = AnyPauseKeeperActive();

        if (pauseGame && _pauseApplied && !keepPaused)
        {
            Time.timeScale = _prevTimeScale;
            _pauseApplied = false;
        }

        if (pauseAudio && _audioApplied && !keepPaused)
        {
            AudioListener.pause = _prevAudioPaused;
            _audioApplied = false;
        }

        if (unlockCursorOnOpen && _cursorApplied && !keepPaused)
        {
            Cursor.lockState = _wasCursorLock;
            Cursor.visible = _wasCursorVisible;
            _cursorApplied = false;
        }
    }

    public void Resume() => Close();

    private bool CloseAnyActiveSubMenu()
    {
        if (subMenusToCloseFirst == null || subMenusToCloseFirst.Length == 0)
            return false;

        bool closed = false;
        for (int i = 0; i < subMenusToCloseFirst.Length; i++)
        {
            var go = subMenusToCloseFirst[i];
            if (go != null && go.activeInHierarchy)
            {
                go.SetActive(false);
                closed = true;
            }
        }
        return closed;
    }

    private bool AnyPauseKeeperActive()
    {
        if (pauseKeepAliveWhileActive == null || pauseKeepAliveWhileActive.Length == 0)
            return false;
        for (int i = 0; i < pauseKeepAliveWhileActive.Length; i++)
        {
            var go = pauseKeepAliveWhileActive[i];
            if (go != null && go.activeInHierarchy)
                return true;
        }
        return false;
    }
}