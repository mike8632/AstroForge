using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueId = Guid.Empty.ToString();

    [Tooltip("If true, this object will not be destroyed by load routines.")]
    [SerializeField] private bool isPersistent = false;

    public string UniqueId => uniqueId;
    public bool IsPersistent => isPersistent;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId) || uniqueId == Guid.Empty.ToString())
        {
            uniqueId = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [ContextMenu("Regenerate ID")]
    private void RegenerateId() => uniqueId = Guid.NewGuid().ToString();
#endif
}
