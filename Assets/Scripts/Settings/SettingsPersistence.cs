using System.IO;
using UnityEngine;

public static class SettingsPersistence
{
    private static string Path => System.IO.Path.Combine(Application.persistentDataPath, "settings.json");

    public static void Save(SettingsData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to save settings: {e.Message}");
        }
    }

    public static SettingsData Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                return JsonUtility.FromJson<SettingsData>(json) ?? SettingsData.Defaults();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load settings, using defaults. {e.Message}");
        }
        return SettingsData.Defaults();
    }
}