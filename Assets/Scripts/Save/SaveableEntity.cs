using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId = "";

    public string UniqueId => uniqueId;

    private static readonly Dictionary<string, SaveableEntity> Registry = new();

    private void Awake()
    {
        EnsureId();
        Register();
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(uniqueId) && Registry.TryGetValue(uniqueId, out var owner) && owner == this)
            Registry.Remove(uniqueId);
    }

    private void EnsureId()
    {
        if (string.IsNullOrEmpty(uniqueId) || uniqueId == Guid.Empty.ToString())
        {
            uniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }
    }

    private void Register()
    {
        if (string.IsNullOrEmpty(uniqueId)) return;
        if (Registry.TryGetValue(uniqueId, out var existing) && existing != null && existing != this)
        {
            string old = uniqueId;
            uniqueId = Guid.NewGuid().ToString();
            Registry[uniqueId] = this;
            Debug.LogWarning($"SaveableEntity: Duplicate UniqueId detected ('{old}'). Generated new id for '{name}'.");
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }
        else
        {
            Registry[uniqueId] = this;
        }
    }

#if UNITY_EDITOR
    // Ensure a GUID is assigned in editor.
    private void OnValidate()
    {
        EnsureId();

        if (!Application.isPlaying)
        {
            var all = FindObjectsOfType<SaveableEntity>(true);
            foreach (var e in all)
            {
                if (e == this) continue;
                if (e.uniqueId == uniqueId)
                {
                    uniqueId = Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(this);
                    break;
                }
            }
        }
    }

    [ContextMenu("Regenerate ID (use if you duplicated objects and IDs collided)")]
    private void RegenerateId()
    {
        uniqueId = Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
    }
#endif
}