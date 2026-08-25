using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSensor : MonoBehaviour
{
    const int MaxHits = 8;

    readonly Collider2D[] _hits = new Collider2D[MaxHits];
    CircleCollider2D _collider;
    Transform _root;
    float _disableTimer;

    public bool IsActive
    {
        get
        {
            if (_disableTimer > 0f)
                return false;

            return HasOverlap();
        }
    }

    void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _root = transform.root;
        _collider.isTrigger = true;
    }

    void Update()
    {
        if (_disableTimer > 0f)
            _disableTimer -= Time.deltaTime;
    }

    public void Disable(float duration)
    {
        _disableTimer = duration;
    }

    bool HasOverlap()
    {
        float scale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y));
        float radius = _collider.radius * scale;

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(transform.position, radius, filter, _hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _hits[i];
            if (hit == null || hit == _collider)
                continue;
            if (hit.transform.root == _root)
                continue;

            return true;
        }

        return false;
    }
}
