using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool fullscreen = false;

    // Store as 0–1 linear values
    public float master = 0.75f;
    public float music = 0.60f;
    public float sfx = 0.80f;

    public static SettingsData Defaults() => new SettingsData();
}