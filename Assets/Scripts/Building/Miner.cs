using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Building))]
public class Miner : MonoBehaviour
{
    [Header("Mining")]
    public ResourceType[] allowed;
    public float cycleSeconds = 1.0f;
    public int perCycle = 1;

    [Header("Output")]
    public Transform outputPoint;
    public GameObject itemPrefab;

    [Header("Detection")]
    [Tooltip("Layers to search for ResourceNode colliders.")]
    public LayerMask resourceLayers = ~0;
    [Tooltip("Search box size factor relative to grid cell or1 unit.")]
    [Range(0.1f, 2f)] public float searchBoxScale = 0.6f;

    private ResourceNode _node;
    private WaitForSeconds _wait;

    private static readonly Collider2D[] _hits = new Collider2D[8];

    private void OnEnable()
    {
        RebuildWait();
        _node = FindNodeUnder();
        StartCoroutine(Run());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cycleSeconds <= 0f) cycleSeconds = 0.01f;
        if (perCycle < 1) perCycle = 1;
        RebuildWait();
    }
#endif

    private void RebuildWait()
    {
        _wait = new WaitForSeconds(cycleSeconds);
    }

    private ResourceNode FindNodeUnder()
    {
        var size = GetCellSize() * searchBoxScale;
        int count = Physics2D.OverlapBoxNonAlloc(transform.position, size, 0f, _hits, resourceLayers);
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (!h) continue;
            var rn = h.GetComponent<ResourceNode>() ?? h.GetComponentInParent<ResourceNode>();
            if (rn == null) continue;

            if (allowed == null || allowed.Length == 0) return rn;
            for (int t = 0; t < allowed.Length; t++)
                if (rn.type.Equals(allowed[t]))
                    return rn;
        }
        return null;
    }

    private Vector2 GetCellSize()
    {
        var grid = GetComponentInParent<Grid>();
        if (grid != null)
        {
            var cs = grid.cellSize;
            if (cs.x <= 0f) cs.x = 1f;
            if (cs.y <= 0f) cs.y = 1f;
            return new Vector2(cs.x, cs.y);
        }
        return Vector2.one;
    }

    private IEnumerator Run()
    {
        while (isActiveAndEnabled)
        {
            yield return _wait;

            if (!_node)
            {
                _node = FindNodeUnder();
                if (!_node) continue;
            }

            int amount = _node.Extract(perCycle);
            if (amount <= 0) continue;

            for (int i = 0; i < amount; i++)
                EmitItem(_node.type);
        }
    }

    private void EmitItem(ResourceType t)
    {
        if (!itemPrefab)
        {
            ResourceBank.Instance?.Add(t, 1);
            return;
        }
        var pos = outputPoint ? outputPoint.position : transform.position + Vector3.right * 0.5f;
        var go = Instantiate(itemPrefab, pos, Quaternion.identity);
        var item = go.GetComponent<ItemEntity>();
        if (item) item.type = t;
    }
}