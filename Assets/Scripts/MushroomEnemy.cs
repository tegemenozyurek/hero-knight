using System.Collections;
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
    [SerializeField] float idleMin = 1.05f;
    [SerializeField] float idleMax = 1.85f;
    [SerializeField] bool spriteFacesRight = true;
    [SerializeField] int maxHealth = 3;
    [SerializeField] float hurtTime = 0.42f;
    [SerializeField] float hurtHop = 0f;
    [SerializeField] float deathHoldTime = 5f;
    [SerializeField] float deathFadeTime = 1.1f;

    Rigidbody2D _body;
    Animator _animator;
    SpriteRenderer _sprite;
    Collider2D _collider;
    int _dir = -1;
    int _health;
    float _idleTime;
    float _hurtLock;
    bool _pendingTurn;
    bool _dead;
    bool _dying;
    bool _usePatrol;
    float _minX;
    float _maxX;

    static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    static readonly int HurtHash = Animator.StringToHash("Hurt");
    static readonly int DeathHash = Animator.StringToHash("Death");
    static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

    public void ConfigurePatrol(float minX, float maxX, int dir)
    {
        _minX = minX;
        _maxX = maxX;
        _usePatrol = maxX > minX + 0.5f;
        _pendingTurn = false;
        _idleTime = 0f;
        moveSpeed *= Random.Range(0.9f, 1.14f);

        if (_usePatrol)
        {
            float x = transform.position.x;
            float toMin = x - _minX;
            float toMax = _maxX - x;
            _dir = toMax >= toMin ? 1 : -1;
        }
        else
        {
            _dir = dir >= 0 ? 1 : -1;
        }
    }

    public void TakeHit(Transform attacker, float knockback)
    {
        if (_dead || _dying || _hurtLock > 0f)
            return;

        _health -= 1;
        int sign = transform.position.x >= attacker.position.x ? 1 : -1;
        Vector2 velocity = _body.linearVelocity;
        velocity.x = sign * knockback;
        if (velocity.y > 0f)
            velocity.y = 0f;
        velocity.y += hurtHop;
        _body.linearVelocity = velocity;
        _idleTime = 0f;
        _pendingTurn = false;
        _dir = -sign;
        IgnoreAttacker(attacker, true);

        _hurtLock = hurtTime;
        _animator.Play("MushroomTakeHit", 0, 0f);
        _animator.SetTrigger(HurtHash);

        if (_health <= 0)
            _dying = true;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _health = maxHealth;

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

    void Start()
    {
        IgnoreOtherMushrooms();
    }

    void IgnoreOtherMushrooms()
    {
        if (_collider == null)
            return;

        MushroomEnemy[] others = FindObjectsByType<MushroomEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < others.Length; i++)
        {
            MushroomEnemy other = others[i];
            if (other == null || other == this || other._collider == null)
                continue;
            Physics2D.IgnoreCollision(_collider, other._collider, true);
        }
    }

    void Update()
    {
        if (_dead)
            return;

        if (_hurtLock > 0f)
        {
            _hurtLock -= Time.deltaTime;
            if (_hurtLock <= 0f)
            {
                IgnoreAttacker(null, false);
                if (_dying)
                {
                    Die();
                    return;
                }
            }
            UpdateFacing();
            return;
        }

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
        UpdateFacing();
    }

    void FixedUpdate()
    {
        Vector2 velocity = _body.linearVelocity;

        if (_dead)
        {
            velocity.x = 0f;
            _body.linearVelocity = velocity;
            return;
        }

        if (_hurtLock > 0f || _dying)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 28f * Time.fixedDeltaTime);
            if (velocity.y > 0f)
                velocity.y = Mathf.MoveTowards(velocity.y, 0f, 18f * Time.fixedDeltaTime);
            _body.linearVelocity = velocity;
            return;
        }

        if (_idleTime > 0f)
        {
            velocity.x = 0f;
            _body.linearVelocity = velocity;
            return;
        }

        if (ShouldEndWalk())
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

    void UpdateFacing()
    {
        bool movingLeft = _dir < 0;
        if (_hurtLock <= 0f && _idleTime <= 0f && Mathf.Abs(_body.linearVelocity.x) > 0.05f)
            movingLeft = _body.linearVelocity.x < 0f;
        _sprite.flipX = spriteFacesRight ? movingLeft : !movingLeft;
    }

    void Die()
    {
        _dead = true;
        _hurtLock = 0f;
        _animator.Play("Death", 0, 0f);
        _animator.SetTrigger(DeathHash);
        _body.linearVelocity = Vector2.zero;
        _body.gravityScale = 0f;
        _body.bodyType = RigidbodyType2D.Kinematic;
        if (_collider != null)
            _collider.enabled = false;
        IgnoreAttacker(null, false);
        StartCoroutine(DeathFade());
    }

    IEnumerator DeathFade()
    {
        yield return new WaitForSeconds(deathHoldTime);

        Color color = _sprite.color;
        float fade = Mathf.Max(0.15f, deathFadeTime);
        float elapsed = 0f;
        while (elapsed < fade)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fade);
            _sprite.color = color;
            yield return null;
        }

        color.a = 0f;
        _sprite.color = color;
        Destroy(gameObject);
    }

    void IgnoreAttacker(Transform attacker, bool ignore)
    {
        if (_collider == null)
            return;

        Transform root = attacker;
        if (root == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
                return;
            root = player.transform;
        }

        Collider2D[] cols = root.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null || cols[i].isTrigger)
                continue;
            Physics2D.IgnoreCollision(_collider, cols[i], ignore);
        }
    }

    bool ShouldEndWalk()
    {
        if (_usePatrol)
            return ReachedBound();
        return ShouldTurn();
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
            if (hit.GetComponent<MushroomEnemy>() != null)
                continue;
            if (hit.GetComponentInParent<PlayerMovement>() != null)
                continue;
            return true;
        }

        return false;
    }
}
