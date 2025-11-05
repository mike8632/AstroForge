using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public string componentType;
        public string stateType;
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
    private static string TempPath => Path.Combine(SaveFolder, "slot1.tmp");
    private static string BackupPath => Path.Combine(SaveFolder, "slot1.bak");

    public bool SaveExists() => File.Exists(SavePath);

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            if (File.Exists(BackupPath))
                File.Delete(BackupPath);
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

                    var compType = s.GetType().AssemblyQualifiedName;
                    var stateType = state.GetType().AssemblyQualifiedName;

                    record.components.Add(new ComponentState
                    {
                        componentType = compType,
                        stateType = stateType,
                        type = stateType, 
                        json = JsonUtility.ToJson(state)
                    });
                }

                if (record.components.Count > 0)
                    save.objects.Add(record);
            }

            var json = JsonUtility.ToJson(save, true);
            File.WriteAllText(TempPath, json);

            try
            {
                if (File.Exists(SavePath))
                    File.Replace(TempPath, SavePath, BackupPath);
                else
                    File.Move(TempPath, SavePath);
            }
            catch
            {
                File.Copy(TempPath, SavePath, true);
                File.Delete(TempPath);
            }

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
                QueueLoadOnNextScene();
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


                var compMap = new Dictionary<string, ComponentState>();
                foreach (var comp in obj.components)
                {
                    var key = !string.IsNullOrEmpty(comp.componentType) ? comp.componentType : comp.type;
                    if (!string.IsNullOrEmpty(key))
                        compMap[key] = comp;
                }

                foreach (var s in saveables)
                {
                    var key = s.GetType().AssemblyQualifiedName;
                    if (key == null) continue;
                    if (!compMap.TryGetValue(key, out var compState)) continue;

                    var tName = !string.IsNullOrEmpty(compState.stateType) ? compState.stateType : compState.type;
                    var t = ResolveType(tName);
                    if (t == null) { Debug.LogWarning($"SaveManager: Missing type {tName}"); continue; }

                    object stateObj = null;
                    try { stateObj = JsonUtility.FromJson(compState.json, t); }
                    catch (Exception ex) { Debug.LogWarning($"SaveManager: JSON parse error: {ex.Message}"); }
                    if (stateObj == null) continue;

                    try { s.RestoreState(stateObj); }
                    catch (Exception ex) { Debug.LogWarning($"SaveManager: Restore error on {entity.name}: {ex.Message}"); }
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

    private static Type ResolveType(string qualifiedName)
    {
        if (string.IsNullOrEmpty(qualifiedName)) return null;
        var t = Type.GetType(qualifiedName);
        if (t != null) return t;
 
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(qualifiedName);
            if (t != null) return t;
        }

        var nameOnly = qualifiedName.Split(',')[0];
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(nameOnly);
            if (t != null) return t;
        }
        return null;
    }
}