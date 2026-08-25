using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class MushroomEnemy : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1.45f;
    [SerializeField] float wallCheckDistance = 0.12f;
    [SerializeField] float edgeCheckDistance = 0.28f;
    [SerializeField] float idleMin = 0.7f;
    [SerializeField] float idleMax = 1.5f;
    [SerializeField] bool spriteFacesRight = true;

    Rigidbody2D _body;
    Animator _animator;
    SpriteRenderer _sprite;
    Collider2D _collider;
    int _dir = -1;
    float _idleTime;
    bool _pendingTurn;
    bool _usePatrol;
    float _minX;
    float _maxX;

    static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

    public void ConfigurePatrol(float minX, float maxX, int dir)
    {
        _minX = minX;
        _maxX = maxX;
        _usePatrol = maxX > minX + 0.5f;
        _dir = dir >= 0 ? 1 : -1;
        _idleTime = Random.Range(0.25f, 0.9f);
        _pendingTurn = false;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        _body.freezeRotation = true;
        _body.gravityScale = 3.5f;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        _body.linearDamping = 0f;

        PhysicsMaterial2D material = new PhysicsMaterial2D("MushroomNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        _body.sharedMaterial = material;
        _collider.sharedMaterial = material;
    }

    void Update()
    {
        if (_idleTime > 0f)
        {
            _idleTime -= Time.deltaTime;
            if (_idleTime <= 0f && _pendingTurn)
            {
                _dir = -_dir;
                _pendingTurn = false;
            }
        }

        bool walking = _idleTime <= 0f;
        _animator.SetInteger(AnimStateHash, walking ? 1 : 0);

        bool movingLeft = _dir < 0;
        if (walking && Mathf.Abs(_body.linearVelocity.x) > 0.05f)
            movingLeft = _body.linearVelocity.x < 0f;
        _sprite.flipX = spriteFacesRight ? movingLeft : !movingLeft;
    }

    void FixedUpdate()
    {
        Vector2 velocity = _body.linearVelocity;

        if (_idleTime > 0f)
        {
            velocity.x = 0f;
            _body.linearVelocity = velocity;
            return;
        }

        if (ReachedBound() || ShouldTurn())
        {
            velocity.x = 0f;
            _body.linearVelocity = velocity;
            _idleTime = Random.Range(idleMin, idleMax);
            _pendingTurn = true;
            return;
        }

        velocity.x = _dir * moveSpeed;
        _body.linearVelocity = velocity;
    }

    bool ReachedBound()
    {
        if (!_usePatrol)
            return false;
        return (_dir < 0 && transform.position.x <= _minX) ||
               (_dir > 0 && transform.position.x >= _maxX);
    }

    bool ShouldTurn()
    {
        Bounds bounds = _collider.bounds;
        float midY = bounds.center.y;
        float footY = bounds.min.y + 0.04f;
        float frontX = bounds.center.x + _dir * (bounds.extents.x + 0.02f);

        if (HasSolid(new Vector2(frontX, midY), new Vector2(_dir, 0f), wallCheckDistance))
            return true;

        return !HasSolid(new Vector2(frontX, footY), Vector2.down, edgeCheckDistance);
    }

    bool HasSolid(Vector2 origin, Vector2 direction, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;

        int count = Physics2D.Raycast(origin, direction, filter, Hits, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = Hits[i].collider;
            if (hit == null || hit == _collider)
                continue;
            if (hit.attachedRigidbody == _body)
                continue;
            return true;
        }

        return false;
    }
}
