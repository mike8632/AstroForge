using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Events;

public class SimpleSettingsMenu : MonoBehaviour
{
    [Header("Auto-wire by name if left empty")]
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Text masterValueText;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Text musicValueText;

    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text sfxValueText;

    [Header("Optional AudioMixer (leave empty to use AudioListener)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam  = "MusicVolume";
    [SerializeField] private string sfxParam    = "SFXVolume";

    [Header("Go Back")]
    public UnityEvent onGoBack;

    // PlayerPrefs keys
    const string KeyResW    = "settings_res_w";
    const string KeyResH    = "settings_res_h";
    const string KeyFull    = "settings_fullscreen";
    const string KeyVolMst  = "settings_vol_master";
    const string KeyVolMus  = "settings_vol_music";
    const string KeyVolSfx  = "settings_vol_sfx";

    private readonly List<Vector2Int> _resOptions = new List<Vector2Int>();
    private bool _suppress;

    private void Awake()
    {
        AutoWireIfNeeded();
        BuildResolutionList();
        LoadFromPrefs();
        ApplyToUIWithoutEvents();
        HookEvents();
        ApplyRuntime(); // make sure the game actually changes at startup
    }

    private void OnDestroy() => UnhookEvents();

    private void AutoWireIfNeeded()
    {
        if (!resolutionDropdown)
            resolutionDropdown = GameObject.Find("ResolutionDropdown")?.GetComponent<Dropdown>();
        if (!fullscreenToggle)
            fullscreenToggle = GameObject.Find("FullscreenToggle")?.GetComponent<Toggle>();

        if (!masterSlider)
            masterSlider = GameObject.Find("MasterSlider")?.GetComponent<Slider>();
        if (!masterValueText)
            masterValueText = GameObject.Find("MasterValueText")?.GetComponent<Text>();

        if (!musicSlider)
            musicSlider = GameObject.Find("MusicSlider")?.GetComponent<Slider>();
        if (!musicValueText)
            musicValueText = GameObject.Find("MusicValueText")?.GetComponent<Text>();

        if (!sfxSlider)
            sfxSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();
        if (!sfxValueText)
            sfxValueText = GameObject.Find("SFXValueText")?.GetComponent<Text>();
    }

    private void BuildResolutionList()
    {
        if (!resolutionDropdown) return;

        resolutionDropdown.ClearOptions();
        _resOptions.Clear();

        var seen = new HashSet<string>();
        foreach (var r in Screen.resolutions)
        {
            var key = r.width + "x" + r.height;
            if (seen.Add(key))
                _resOptions.Add(new Vector2Int(r.width, r.height));
        }

        // Fallback: at least current resolution
        if (_resOptions.Count == 0)
            _resOptions.Add(new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height));

        var labels = new List<string>(_resOptions.Count);
        foreach (var v in _resOptions) labels.Add($"{v.x}x{v.y}");
        resolutionDropdown.AddOptions(labels);
    }

    private void HookEvents()
    {
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (masterSlider) masterSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
        if (musicSlider)  musicSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener(_ => OnVolumeChanged());
    }

    private void UnhookEvents()
    {
        if (resolutionDropdown) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

        if (masterSlider) masterSlider.onValueChanged.RemoveListener(_ => OnVolumeChanged());
        if (musicSlider)  musicSlider.onValueChanged.RemoveListener(_ => OnVolumeChanged());
        if (sfxSlider)    sfxSlider.onValueChanged.RemoveListener(_ => OnVolumeChanged());
    }

    private void ApplyToUIWithoutEvents()
    {
        _suppress = true;

        // Resolution
        if (resolutionDropdown)
        {
            var savedW = PlayerPrefs.GetInt(KeyResW, Screen.currentResolution.width);
            var savedH = PlayerPrefs.GetInt(KeyResH, Screen.currentResolution.height);
            var idx = _resOptions.FindIndex(r => r.x == savedW && r.y == savedH);
            if (idx < 0) idx = Mathf.Max(0, _resOptions.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(idx);
        }

        // Fullscreen
        if (fullscreenToggle)
            fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KeyFull, 0) == 1);

        // Sliders (0..100 UI)
        SetSlider(masterSlider, PlayerPrefs.GetFloat(KeyVolMst, 0.75f));
        SetSlider(musicSlider,  PlayerPrefs.GetFloat(KeyVolMus, 0.60f));
        SetSlider(sfxSlider,    PlayerPrefs.GetFloat(KeyVolSfx, 0.80f));

        UpdatePercentLabels();

        _suppress = false;
    }

    private static void SetSlider(Slider s, float linear01)
    {
        if (!s) return;
        s.SetValueWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(linear01) * 100f));
    }

    private float GetLinear01(Slider s) => s ? Mathf.Clamp01(s.value / 100f) : 0f;

    private void LoadFromPrefs() { /* all handled in ApplyToUIWithoutEvents */ }

    private void OnResolutionChanged(int idx)
    {
        if (_suppress || _resOptions.Count == 0) return;
        var r = _resOptions[Mathf.Clamp(idx, 0, _resOptions.Count - 1)];
        PlayerPrefs.SetInt(KeyResW, r.x);
        PlayerPrefs.SetInt(KeyResH, r.y);
        ApplyScreen();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        if (_suppress) return;
        PlayerPrefs.SetInt(KeyFull, isOn ? 1 : 0);
        ApplyScreen();
    }

    private void OnVolumeChanged()
    {
        if (_suppress) return;

        var master = GetLinear01(masterSlider);
        var music  = GetLinear01(musicSlider);
        var sfx    = GetLinear01(sfxSlider);

        PlayerPrefs.SetFloat(KeyVolMst, master);
        PlayerPrefs.SetFloat(KeyVolMus, music);
        PlayerPrefs.SetFloat(KeyVolSfx, sfx);

        UpdatePercentLabels();
        ApplyAudio(master, music, sfx);
    }

    private void UpdatePercentLabels()
    {
        if (masterValueText && masterSlider) masterValueText.text = $"{Mathf.RoundToInt(masterSlider.value)}%";
        if (musicValueText  && musicSlider)  musicValueText.text  = $"{Mathf.RoundToInt(musicSlider.value)}%";
        if (sfxValueText    && sfxSlider)    sfxValueText.text    = $"{Mathf.RoundToInt(sfxSlider.value)}%";
    }

    private void ApplyRuntime()
    {
        ApplyScreen();
        var master = PlayerPrefs.GetFloat(KeyVolMst, 0.75f);
        var music  = PlayerPrefs.GetFloat(KeyVolMus, 0.60f);
        var sfx    = PlayerPrefs.GetFloat(KeyVolSfx, 0.80f);
        ApplyAudio(master, music, sfx);
    }

    private void ApplyScreen()
    {
        var w = PlayerPrefs.GetInt(KeyResW, Screen.currentResolution.width);
        var h = PlayerPrefs.GetInt(KeyResH, Screen.currentResolution.height);
        var full = PlayerPrefs.GetInt(KeyFull, 0) == 1;
        var mode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(w, h, mode);
    }

    private void ApplyAudio(float master, float music, float sfx)
    {
        if (audioMixer)
        {
            if (!string.IsNullOrEmpty(masterParam)) audioMixer.SetFloat(masterParam, LinearToDb(master));
            if (!string.IsNullOrEmpty(musicParam))  audioMixer.SetFloat(musicParam,  LinearToDb(music));
            if (!string.IsNullOrEmpty(sfxParam))    audioMixer.SetFloat(sfxParam,    LinearToDb(sfx));
        }
        else
        {
            // Simple fallback: master controls global volume
            AudioListener.volume = Mathf.Clamp01(master);
        }
    }

    private static float LinearToDb(float x)
    {
        const float muteFloor = -80f;
        if (x <= 0.0001f) return muteFloor;
        return Mathf.Log10(Mathf.Clamp(x, 0.0001f, 1f)) * 20f;
    }

    // Hook this from your "Go Back" button.
    public void OnGoBack() => onGoBack?.Invoke();
}