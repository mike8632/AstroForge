using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceBank : MonoBehaviour
{
    public static ResourceBank Instance { get; private set; }

    [System.Serializable]
    public class ChangedEvent : UnityEvent { }
    public ChangedEvent onChanged = new ChangedEvent();

    private readonly Dictionary<ResourceType, int> _counts = new();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int Get(ResourceType t) => _counts.TryGetValue(t, out var v) ? v : 0;

    public void Set(ResourceType t, int value)
    {
        _counts[t] = Mathf.Max(0, value);
        onChanged.Invoke();
    }

    public void Add(ResourceType t, int delta)
    {
        Set(t, Get(t) + delta);
    }
}