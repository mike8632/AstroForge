using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ItemEntity : MonoBehaviour
{
    public ResourceType type;
    public float speed = 3f;
    [SerializeField, Tooltip("How fast the item recenters to the belt middle.")]
    private float centerSnapSpeed = 12f;

    [Header("Idle Despawn")]
    [SerializeField, Tooltip("Seconds an item can remain idle (off-belts, not touching buildings) before it is destroyed.")]
    private float idleDespawnSeconds = 10f;
    [SerializeField, Tooltip("Minimum movement speed (units/sec) to count as moving and reset the idle timer.")]
    private float idleSpeedThreshold = 0.02f;

    private Rigidbody2D _rb;
    private Vector2 _dir;
    private int _beltContacts;
    private int _buildingContacts;
    private Belt _currentBelt;

    private Vector2 _lastPos;
    private float _idleTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        _lastPos = _rb.position;
        _idleTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (_rb == null) return;

        var posBefore = _rb.position;

        if (_currentBelt != null)
        {
            // Move forward along belt and snap laterally toward belt centerline
            var flow = _currentBelt.DirectionVector;
            flow = flow.sqrMagnitude > 0f ? flow.normalized : Vector2.right;
            var pos = posBefore;
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
            _rb.MovePosition(posBefore + _dir * speed * Time.fixedDeltaTime);
        }

        // Idle-despawn logic
        var newPos = _rb.position; // updated by physics before next FixedUpdate
        float distance = (newPos - _lastPos).magnitude;
        float vel = distance / Mathf.Max(Time.fixedDeltaTime, 1e-6f);
        bool moving = vel > idleSpeedThreshold;
        bool onBelt = _currentBelt != null || _beltContacts > 0;
        bool touchingBuilding = _buildingContacts > 0;

        if (moving || onBelt || touchingBuilding)
        {
            _idleTimer = 0f;
        }
        else
        {
            _idleTimer += Time.fixedDeltaTime;
            if (_idleTimer >= idleDespawnSeconds)
            {
                Destroy(gameObject);
                return;
            }
        }

        _lastPos = newPos;
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
            _idleTimer = 0f;
        }
        else if (other.TryGetComponent<Building>(out var _))
        {
            _buildingContacts++;
            _idleTimer = 0f;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent<Belt>(out var belt))
        {
            _currentBelt = belt;
            SetDirection(belt.DirectionVector);
            _idleTimer = 0f;
        }
        else if (other.TryGetComponent<Building>(out var _))
        {
            // Still touching a building; keep idle timer reset
            _idleTimer = 0f;
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
        else if (other.TryGetComponent<Building>(out var _))
        {
            _buildingContacts = Mathf.Max(0, _buildingContacts - 1);
        }
    }
}