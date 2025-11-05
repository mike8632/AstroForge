using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ItemEntity : MonoBehaviour
{
    public ResourceType type;
    public float speed = 3f;

    private Rigidbody2D _rb;
    private Vector2 _dir;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.isKinematic = true;
        _rb.gravityScale = 0;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        _rb.MovePosition(_rb.position + _dir * speed * Time.deltaTime);
    }

    public void SetDirection(Vector2 d) => _dir = d.normalized;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Collectors handle pickup; belts set direction
        if (other.TryGetComponent<Belt>(out var belt))
        {
            SetDirection(belt.DirectionVector);
        }
        else if (other.TryGetComponent<Collector>(out var collector))
        {
            collector.Collect(this);
        }
    }
}