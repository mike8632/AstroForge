using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ItemEntity : MonoBehaviour
{
    public ResourceType type;
    public float speed = 3f;

    private Rigidbody2D _rb;
    private Vector2 _dir;
    private int _beltContacts;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;
        if (_dir.sqrMagnitude > 0f)
            _rb.MovePosition(_rb.position + _dir * speed * Time.fixedDeltaTime);
    }

    public void SetDirection(Vector2 d)
    {
        _dir = d.sqrMagnitude > 0f ? d.normalized : Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Belt>(out var belt))
        {
            _beltContacts++;
            SetDirection(belt.DirectionVector);
        }
        else if (other.TryGetComponent<Collector>(out var collector))
        {
            collector.Collect(this);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent<Belt>(out var belt))
        {
            SetDirection(belt.DirectionVector);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<Belt>(out var _))
        {
            _beltContacts = Mathf.Max(0, _beltContacts - 1);
            if (_beltContacts == 0)
            {
                // Optionally stop drifting when off belts
                // SetDirection(Vector2.zero);
            }
        }
    }
}