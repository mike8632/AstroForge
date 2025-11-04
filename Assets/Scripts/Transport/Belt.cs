using UnityEngine;

public enum BeltDir { Up, Right, Down, Left }

[RequireComponent(typeof(BoxCollider2D))]
public class Belt : MonoBehaviour
{
    public BeltDir direction = BeltDir.Right;

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
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.9f;
    }
}