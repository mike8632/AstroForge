using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Collector : MonoBehaviour
{
    [SerializeField] private bool autoCollect = true;

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.9f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col.size == Vector2.zero)
                col.size = Vector2.one * 0.9f;
        }
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoCollect) return;

        if (other.TryGetComponent<ItemEntity>(out var item))
        {
            Collect(item);
        }
    }

    public void Collect(ItemEntity item)
    {
        if (item == null) return;


        if (!item.gameObject.activeInHierarchy)
            return;

        ResourceBank.Instance?.Add(item.type, 1);

        Destroy(item.gameObject);
    }
}
