using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class ResourceBank : MonoBehaviour
{
    public static ResourceBank Instance { get; private set; }

    // Fired whenever anything changes (good for HUD refresh)
    [System.Serializable] public class ChangedEvent : UnityEvent { }
    public ChangedEvent onChanged = new ChangedEvent();

    // Fired for a specific resource change (type + new value)
    [System.Serializable] public class ChangedOneEvent : UnityEvent<ResourceType, int> { }
    public ChangedOneEvent onChangedOne = new ChangedOneEvent();

    [Header("Startup / Debug")]
    [SerializeField] private bool startWithTestResources = true;
    [SerializeField] private List<ResourceAmount> initialResources = new()
    {
        new ResourceAmount(ResourceType.Stone,15),
        new ResourceAmount(ResourceType.Coal,8),
        new ResourceAmount(ResourceType.IronOre,5),
        new ResourceAmount(ResourceType.CopperOre,3),
        new ResourceAmount(ResourceType.GoldOre,1),
    };

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

        if (startWithTestResources)
        {
            BeginBatch();
            foreach (var r in initialResources)
                Add(r.type, r.amount);
            EndBatch();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Core API
    public int Get(ResourceType t) => _counts.TryGetValue(t, out var v) ? v :0;

    public void Set(ResourceType t, int value)
    {
        int clamped = Mathf.Max(0, value);
        int old = Get(t);
        if (old == clamped) return;
        _counts[t] = clamped;
        onChangedOne.Invoke(t, clamped);
        if (_batchDepth ==0) onChanged.Invoke();
    }

    public void Add(ResourceType t, int delta)
    {
        if (delta ==0) return;
        Set(t, Get(t) + delta);
    }

    // Helpers
    public bool CanAfford(ResourceType t, int cost) => Get(t) >= Mathf.Max(0, cost);

    public bool TrySpend(ResourceType t, int cost)
    {
        cost = Mathf.Max(0, cost);
        if (!CanAfford(t, cost)) return false;
        Set(t, Get(t) - cost);
        return true;
    }

    public void Spend(ResourceType t, int amount) => Set(t, Get(t) - Mathf.Max(0, amount));

    public void AddMany(IEnumerable<ResourceAmount> list)
    {
        BeginBatch();
        foreach (var r in list) Add(r.type, r.amount);
        EndBatch();
    }

    public bool CanAffordMany(IEnumerable<ResourceAmount> costs)
    {
        foreach (var c in costs)
            if (!CanAfford(c.type, c.amount)) return false;
        return true;
    }

    public bool TrySpendMany(IEnumerable<ResourceAmount> costs)
    {
        // Materialize to avoid double enumeration surprises
        var temp = costs is List<ResourceAmount> l ? l : new List<ResourceAmount>(costs);
        if (!CanAffordMany(temp)) return false;
        BeginBatch();
        foreach (var c in temp) Spend(c.type, c.amount);
        EndBatch();
        return true;
    }

    public void Clear()
    {
        if (_counts.Count ==0) return;
        _counts.Clear();
        onChanged.Invoke();
    }

    public void BeginBatch() { _batchDepth++; }
    public void EndBatch()
    {
        if (_batchDepth <=0) return;
        _batchDepth--;
        if (_batchDepth ==0) onChanged.Invoke();
    }

    [System.Serializable]
    public struct ResourceAmount
    {
        public ResourceType type;
        public int amount;
        public ResourceAmount(ResourceType type, int amount) { this.type = type; this.amount = amount; }
    }
}
