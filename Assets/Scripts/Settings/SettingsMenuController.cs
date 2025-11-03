using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private TextMeshProUGUI masterPercentLabel;
    [SerializeField] private TextMeshProUGUI musicPercentLabel;
    [SerializeField] private TextMeshProUGUI sfxPercentLabel;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    private readonly List<Vector2Int> _resolutions = new();
    private SettingsData _data;

    private void Awake()
    {
        PopulateResolutions();
        LoadSettings();
        ApplyToUI(_data);
        HookUI();
    }

    private void OnDestroy()
    {
        UnhookUI();
    }

    private void HookUI()
    {
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        masterSlider.onValueChanged.AddListener(OnMasterSlider);
        musicSlider.onValueChanged.AddListener(OnMusicSlider);
        sfxSlider.onValueChanged.AddListener(OnSfxSlider);
    }

    private void UnhookUI()
    {
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        masterSlider.onValueChanged.RemoveListener(OnMasterSlider);
        musicSlider.onValueChanged.RemoveListener(OnMusicSlider);
        sfxSlider.onValueChanged.RemoveListener(OnSfxSlider);
    }

    private void PopulateResolutions()
    {
        resolutionDropdown.ClearOptions();
        _resolutions.Clear();

        // Unique width x height pairs; order ascending
        var unique = new HashSet<(int w, int h)>();
        foreach (var r in Screen.resolutions.OrderBy(r => r.width).ThenBy(r => r.height))
        {
            if (unique.Add((r.width, r.height)))
            {
                _resolutions.Add(new Vector2Int(r.width, r.height));
            }
        }

        // Fallback if nothing reported
        if (_resolutions.Count == 0)
        {
            var w = Screen.currentResolution.width;
            var h = Screen.currentResolution.height;
            _resolutions.Add(new Vector2Int(w, h));
        }

        var options = _resolutions.Select(r => $"{r.x}x{r.y}").ToList();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSettings()
    {
        _data = SettingsPersistence.Load();

        // If saved resolution not in list, fall back to current
        if (!_resolutions.Any(r => r.x == _data.resolutionWidth && r.y == _data.resolutionHeight))
        {
            _data.resolutionWidth = Screen.currentResolution.width;
            _data.resolutionHeight = Screen.currentResolution.height;
        }
    }

    private void ApplyToUI(SettingsData data)
    {
        // Resolution dropdown selection
        var idx = _resolutions.FindIndex(r => r.x == data.resolutionWidth && r.y == data.resolutionHeight);
        if (idx < 0) idx = Mathf.Max(0, _resolutions.Count - 1);
        resolutionDropdown.SetValueWithoutNotify(idx);

        fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);

        // Sliders use 0..100 for UX; we store 0..1
        SetSlider(masterSlider, data.master);
        SetSlider(musicSlider, data.music);
        SetSlider(sfxSlider, data.sfx);

        UpdatePercentLabels();
        ApplyRuntime(data); // Ensure runtime state matches UI at startup
    }

    private void SetSlider(Slider s, float linear01)
    {
        s.SetValueWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(linear01) * 100f));
    }

    private float SliderToLinear(Slider s) => Mathf.Clamp01(s.value / 100f);

    private void UpdatePercentLabels()
    {
        if (masterPercentLabel) masterPercentLabel.text = $"{Mathf.RoundToInt(masterSlider.value)}%";
        if (musicPercentLabel)  musicPercentLabel.text  = $"{Mathf.RoundToInt(musicSlider.value)}%";
        if (sfxPercentLabel)    sfxPercentLabel.text    = $"{Mathf.RoundToInt(sfxSlider.value)}%";
    }

    // UI callbacks
    private void OnResolutionChanged(int index)
    {
        var r = _resolutions[Mathf.Clamp(index, 0, _resolutions.Count - 1)];
        _data.resolutionWidth = r.x;
        _data.resolutionHeight = r.y;
        ApplyScreen(_data);
        Save();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        _data.fullscreen = isOn;
        ApplyScreen(_data);
        Save();
    }

    private void OnMasterSlider(float _)
    {
        _data.master = SliderToLinear(masterSlider);
        ApplyAudio(_data);
        UpdatePercentLabels();
        Save();
    }

    private void OnMusicSlider(float _)
    {
        _data.music = SliderToLinear(musicSlider);
        ApplyAudio(_data);
        UpdatePercentLabels();
        Save();
    }

    private void OnSfxSlider(float _)
    {
        _data.sfx = SliderToLinear(sfxSlider);
        ApplyAudio(_data);
        UpdatePercentLabels();
        Save();
    }

    // Applying settings
    private void ApplyRuntime(SettingsData data)
    {
        ApplyScreen(data);
        ApplyAudio(data);
    }

    private void ApplyScreen(SettingsData data)
    {
        var mode = data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode);
    }

    private void ApplyAudio(SettingsData data)
    {
        // Convert 0..1 to dB; clamp zero to a reasonable mute floor
        audioMixer.SetFloat(masterParam, LinearToDb(data.master));
        audioMixer.SetFloat(musicParam,  LinearToDb(data.music));
        audioMixer.SetFloat(sfxParam,    LinearToDb(data.sfx));
    }

    // Utility: linear [0..1] -> dB
    private static float LinearToDb(float linear)
    {
        const float muteFloor = -80f; // dB
        if (linear <= 0.0001f) return muteFloor;
        return Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
    }

    private void Save() => SettingsPersistence.Save(_data);

    // Optional: hook this to your "Go Back" button
    public void OnGoBack()
    {
        // If this is a modal/panel:
        // gameObject.SetActive(false);

        // Or if it's a scene transition:
        // SceneManager.LoadScene("MainMenu");
    }
}