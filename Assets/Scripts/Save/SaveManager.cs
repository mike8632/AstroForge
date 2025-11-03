using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoLoadOnSceneStart = false;

    [Header("Continue button flag (PlayerPrefs)")]
    [SerializeField] private string continueFlagKey = "save_exists";

    [Serializable]
    private class ComponentState
    {
        public string type;
        public string json;
    }

    [Serializable]
    private class ObjectRecord
    {
        public string id;
        public List<ComponentState> components = new();
    }

    [Serializable]
    private class SaveFile
    {
        public string version = "1";
        public string sceneName;
        public string savedAtUtc;
        public List<ObjectRecord> objects = new();
    }

    private static bool _pendingLoadOnNextScene = false;

    public static void ClearQueuedLoad() => _pendingLoadOnNextScene = false;

    public static void QueueLoadOnNextScene() => _pendingLoadOnNextScene = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_pendingLoadOnNextScene || autoLoadOnSceneStart)
        {
            _pendingLoadOnNextScene = false;
            TryLoadGame();
        }
    }

    private static string SaveFolder => Path.Combine(Application.persistentDataPath, "saves");
    private static string SavePath => Path.Combine(SaveFolder, "slot1.json");

    public bool SaveExists() => File.Exists(SavePath);

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveManager: Delete failed: {e.Message}");
        }
        PlayerPrefs.SetInt(continueFlagKey, 0);
        PlayerPrefs.Save();
    }

    public void SaveGame()
    {
        try
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);

            var save = new SaveFile
            {
                sceneName = SceneManager.GetActiveScene().name,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                objects = new List<ObjectRecord>()
            };

            var entities = FindObjectsOfType<SaveableEntity>(true);
            foreach (var entity in entities)
            {
                var saveables = entity.GetComponents<ISaveable>();
                if (saveables == null || saveables.Length == 0) continue;

                var record = new ObjectRecord { id = entity.UniqueId };

                foreach (var s in saveables)
                {
                    var state = s.CaptureState();
                    if (state == null) continue;

                    record.components.Add(new ComponentState
                    {
                        type = state.GetType().AssemblyQualifiedName,
                        json = JsonUtility.ToJson(state)
                    });
                }

                if (record.components.Count > 0)
                    save.objects.Add(record);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));

            PlayerPrefs.SetInt(continueFlagKey, 1);
            PlayerPrefs.Save();

            Debug.Log($"SaveManager: Saved {save.objects.Count} objects to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: Save failed: {e}");
        }
    }

    public bool TryLoadGame()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("SaveManager: No save file found.");
                return false;
            }

            var save = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
            if (save == null)
            {
                Debug.LogWarning("SaveManager: Save file unreadable.");
                return false;
            }

            var current = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(save.sceneName) && save.sceneName != current)
            {
                Debug.Log($"SaveManager: Loading saved scene '{save.sceneName}' (current '{current}').");
                QueueLoadOnNextScene(); // will load data after the scene switches
                SceneManager.LoadScene(save.sceneName);
                return true;
            }

            var lookup = FindObjectsOfType<SaveableEntity>(true).ToDictionary(e => e.UniqueId, e => e);
            int restoredObjects = 0;

            foreach (var obj in save.objects)
            {
                if (!lookup.TryGetValue(obj.id, out var entity)) continue;
                var saveables = entity.GetComponents<ISaveable>();
                if (saveables == null || saveables.Length == 0) continue;

                foreach (var comp in obj.components)
                {
                    var type = Type.GetType(comp.type);
                    if (type == null) { Debug.LogWarning($"SaveManager: Missing type {comp.type}"); continue; }

                    var state = JsonUtility.FromJson(comp.json, type);

                    foreach (var s in saveables)
                    {
                        try { s.RestoreState(state); }
                        catch (InvalidCastException) { /* ignore different state types */ }
                        catch (Exception ex) { Debug.LogWarning($"SaveManager: Restore error on {entity.name}: {ex.Message}"); }
                    }
                }

                restoredObjects++;
            }

            Debug.Log($"SaveManager: Loaded {restoredObjects} objects from save.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: Load failed: {e}");
            return false;
        }
    }
}