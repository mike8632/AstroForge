using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ResourcePileRandomizer : MonoBehaviour
{
    [Range(0f, 20f)] public float rotationVariance = 10f;   
    [Range(0f, 0.25f)] public float scaleVariance = 0.08f; 
    [Tooltip("Optional seed to vary patterns between different prefabs/assets.")]
    public int seed = 0;

    [Header("Filter")]
    [Tooltip("Only randomize when a ResourceNode exists on this object or its parents.")]
    public bool onlyOnResourceNodes = true;

    Vector3Int _lastCell;
    bool _hasLast;

    void OnEnable() { Apply(); }
#if UNITY_EDITOR
    void OnValidate() { Apply(); }
#endif

    void LateUpdate()
    {
        var cell = GetCell();
        if (!_hasLast || cell != _lastCell)
        {
            Apply();
            _lastCell = cell;
            _hasLast = true;
        }
    }

    void Apply()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;
#endif
        if (onlyOnResourceNodes)
        {
            var rn = GetComponent<ResourceNode>() ?? GetComponentInParent<ResourceNode>();
            if (rn == null) return;
        }

        var cell = GetCell();
        uint h = Hash2D((uint)cell.x, (uint)cell.y) ^ (uint)seed;

        float r01a = To01(h * 2246822519u);
        float r01b = To01(h * 3266489917u);

        float rot = Mathf.Lerp(-rotationVariance, rotationVariance, r01a);
        float scl = 1f + Mathf.Lerp(-scaleVariance, scaleVariance, r01b);

        transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        transform.localScale = new Vector3(scl, scl, 1f);
    }

    Vector3Int GetCell()
    {
        var grid = GetComponentInParent<Grid>();
        if (grid)
        {
            var cell = grid.WorldToCell(transform.position);
            return new Vector3Int(cell.x, cell.y, 0);
        }
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);
        return new Vector3Int(x, y, 0);
    }

    static uint Hash2D(uint x, uint y)
    {
        uint h = 2166136261u;
        h = (h ^ x) * 16777619u;
        h = (h ^ y) * 16777619u;
        return h;
    }
    static float To01(uint v) => (v & 0xFFFFFF) / 16777215f; 

#if UNITY_EDITOR
    [ContextMenu("Refresh Randomization")]
    void Refresh() => Apply();
#endif
}
