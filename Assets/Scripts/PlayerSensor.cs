using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSensor : MonoBehaviour
{
    const int MaxHits = 8;

    readonly Collider2D[] _hits = new Collider2D[MaxHits];
    CircleCollider2D _collider;
    Rigidbody2D _body;
    int _overlapCount;
    float _disableTimer;

    public bool IsActive
    {
        get
        {
            if (_disableTimer > 0f)
                return false;

            return _overlapCount > 0 || HasOverlap();
        }
    }

    void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _body = GetComponentInParent<Rigidbody2D>();
        _collider.isTrigger = true;
    }

    void OnEnable()
    {
        _overlapCount = 0;
    }

    void Update()
    {
        if (_disableTimer > 0f)
            _disableTimer -= Time.deltaTime;
    }

    public void Disable(float duration)
    {
        _disableTimer = duration;
        _overlapCount = 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwnCollider(other))
            return;

        _overlapCount++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsOwnCollider(other))
            return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
    }

    bool HasOverlap()
    {
        if (_collider == null)
            return false;

        float scale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y));
        float radius = Mathf.Max(0.02f, _collider.radius * scale);

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(transform.position, radius, filter, _hits);
        for (int i = 0; i < count; i++)
        {
            if (!IsOwnCollider(_hits[i]))
                return true;
        }

        return false;
    }

    bool IsOwnCollider(Collider2D other)
    {
        if (other == null || other == _collider)
            return true;

        if (_body != null && other.attachedRigidbody == _body)
            return true;

        return other.transform.root == transform.root;
    }
}
