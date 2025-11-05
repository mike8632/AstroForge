using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId = Guid.Empty.ToString();

    public string UniqueId => uniqueId;

#if UNITY_EDITOR
    // Ensure a GUID is assigned in editor.
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId) || uniqueId == Guid.Empty.ToString())
        {
            uniqueId = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [ContextMenu("Regenerate ID (use if you duplicated objects and IDs collided)")]
    private void RegenerateId()
    {
        uniqueId = Guid.NewGuid().ToString();
    }
#endif
}