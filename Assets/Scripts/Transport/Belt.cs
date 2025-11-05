using UnityEngine;

public enum BeltDir { Up, Right, Down, Left }

[RequireComponent(typeof(BoxCollider2D))]
public class Belt : MonoBehaviour
{
    public BeltDir direction = BeltDir.Right;

    [Header("Setup")]
    [SerializeField] private bool autoSizeCollider = true;
    [SerializeField] private bool autoOrientToDirection = true;
    [SerializeField, Range(0.1f, 1.0f)] private float colliderSizeFactor = 0.9f;

    public Vector2 DirectionVector => direction switch
    {
        BeltDir.Up => Vector2.up,
        BeltDir.Right => Vector2.right,
        BeltDir.Down => Vector2.down,
        BeltDir.Left => Vector2.left,
        _ => Vector2.right
    };

    private void Reset()
    {
        ApplyColliderSettings();
        ApplyOrientation();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyColliderSettings();
        ApplyOrientation();
    }
#endif

    private void ApplyColliderSettings()
    {
        var col = GetComponent<BoxCollider2D>();
        if (!col) return;
        col.isTrigger = true;
        col.offset = Vector2.zero;
        if (!autoSizeCollider) return;

        var grid = GetComponentInParent<Grid>();
        if (grid != null)
        {
            var cs = grid.cellSize;
            if (cs.x <= 0f) cs.x = 1f;
            if (cs.y <= 0f) cs.y = 1f;
            col.size = new Vector2(cs.x, cs.y) * Mathf.Clamp01(colliderSizeFactor);
        }
        else
        {
            col.size = Vector2.one * Mathf.Clamp01(colliderSizeFactor);
        }
    }

    private void ApplyOrientation()
    {
        if (!autoOrientToDirection) return;
        float angle = direction switch
        {
            BeltDir.Right => 0f,
            BeltDir.Up => 90f,
            BeltDir.Left => 180f,
            BeltDir.Down => 270f,
            _ => 0f
        };
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 p = transform.position;
        Vector3 d = new Vector3(DirectionVector.x, DirectionVector.y, 0f) * 0.5f;
        Gizmos.DrawLine(p - d * 0.5f, p + d * 0.5f);
        Gizmos.DrawSphere(p + d, 0.05f);
    }
}