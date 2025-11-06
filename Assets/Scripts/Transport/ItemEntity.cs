using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ItemEntity : MonoBehaviour
{
    public ResourceType type;
    public float speed = 3f;
    [SerializeField, Tooltip("How fast the item recenters to the belt middle.")]
    private float centerSnapSpeed = 12f;

    private Rigidbody2D _rb;
    private Vector2 _dir;
    private int _beltContacts;
    private Belt _currentBelt;

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

        if (_currentBelt != null)
        {
            // Move forward along belt and snap laterally toward belt centerline
            var flow = _currentBelt.DirectionVector;
            flow = flow.sqrMagnitude > 0f ? flow.normalized : Vector2.right;
            var pos = _rb.position;
            var center = (Vector2)_currentBelt.transform.position;
            float k = 1f - Mathf.Exp(-centerSnapSpeed * Time.fixedDeltaTime); // smooth factor
            if (Mathf.Abs(flow.x) >= Mathf.Abs(flow.y))
            {
                // Horizontal belt: snap Y toward center
                pos.y = Mathf.Lerp(pos.y, center.y, k);
            }
            else
            {
                // Vertical belt: snap X toward center
                pos.x = Mathf.Lerp(pos.x, center.x, k);
            }
            pos += flow * speed * Time.fixedDeltaTime;
            _rb.MovePosition(pos);
        }
        else if (_dir.sqrMagnitude > 0f)
        {
            // Fallback motion (should be zero after leaving belts per OnTriggerExit)
            _rb.MovePosition(_rb.position + _dir * speed * Time.fixedDeltaTime);
        }
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
            _currentBelt = belt;
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
            _currentBelt = belt;
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
                _currentBelt = null;
                // Stop at the end of belt
                SetDirection(Vector2.zero);
            }
        }
    }
}