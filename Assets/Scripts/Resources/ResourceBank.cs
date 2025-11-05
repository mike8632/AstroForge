using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class ResourceBank : MonoBehaviour
{
    public static ResourceBank Instance { get; private set; }

    [System.Serializable]
    public class ChangedEvent : UnityEvent { }
    public ChangedEvent onChanged = new ChangedEvent();

    [Header("Debug/Seed (optional)")]
    [Tooltip("Seed the bank with some test resources at startup (Editor only by default).")]
    [SerializeField] private bool seedWithTestData = true;

    private readonly Dictionary<ResourceType, int> _counts = new();

    private int _batchDepth;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (seedWithTestData)
        {
            BeginBatch();
            Add(ResourceType.Stone, 15);
            Add(ResourceType.Coal, 8);
            Add(ResourceType.IronOre, 5);
            Add(ResourceType.CopperOre, 3);
            Add(ResourceType.GoldOre, 1);
            EndBatch();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int Get(ResourceType t) => _counts.TryGetValue(t, out var v) ? v : 0;

    public void Set(ResourceType t, int value)
    {
        int newValue = Mathf.Max(0, value);
        int oldValue = Get(t);
        if (oldValue == newValue)
            return;
        _counts[t] = newValue;
        if (_batchDepth == 0)
            onChanged.Invoke();
    }

    public void Add(ResourceType t, int delta)
    {
        Set(t, Get(t) + delta);
    }

    public void BeginBatch()
    {
        _batchDepth++;
    }

    public void EndBatch()
    {
        if (_batchDepth <= 0)
            return;
        _batchDepth--;
        if (_batchDepth == 0)
            onChanged.Invoke();
    }
}
