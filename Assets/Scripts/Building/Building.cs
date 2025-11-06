using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Building : MonoBehaviour
{
    [Tooltip("Footprint in cells for overlap checks (should match BuildableDefinition).")]
    public Vector2Int footprint = new Vector2Int(1, 1);

    [Tooltip("Optional: layer mask used by placement checks.")]
    public LayerMask blockingLayers;

    [Header("Collider")]
    [Tooltip("Automatically size the BoxCollider2D to match the tile grid cell size x footprint.")]
    [SerializeField] private bool autoSizeCollider = true;

    public void ConfigureFrom(BuildableDefinition def)
    {
        if (!def) return;
        footprint = def.footprint;
        if (autoSizeCollider)
            ApplyFootprintToCollider();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (footprint.x < 1) footprint.x = 1;
        if (footprint.y < 1) footprint.y = 1;
        if (autoSizeCollider)
            ApplyFootprintToCollider();
    }
#endif

    private void ApplyFootprintToCollider()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        // Try to use the nearest Grid cell size if available
        var grid = GetComponentInParent<Grid>();
        if (grid != null)
        {
            var cs = grid.cellSize;
            if (cs.x <= 0f) cs.x = 1f;
            if (cs.y <= 0f) cs.y = 1f;
            col.size = new Vector2(footprint.x * cs.x, footprint.y * cs.y);
        }
        else
        {
            col.size = new Vector2(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        }
        col.offset = Vector2.zero;
    }
}