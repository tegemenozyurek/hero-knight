using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Combo")]
    [SerializeField] float comboResetTime = 0.9f;
    [SerializeField] float comboDelay = 0.16f;
    [SerializeField] float lightLockTime = 0.42f;
    [SerializeField] float finisherLockTime = 0.48f;
    [SerializeField] float lightLunge = 2.4f;

    [Header("Heavy")]
    [SerializeField] float heavyLockTime = 0.58f;
    [SerializeField] float heavyLunge = 3.6f;

    [Header("Hitbox")]
    [SerializeField] Vector2 hitboxSize = new Vector2(1.4f, 1.15f);
    [SerializeField] float hitboxForward = 1.05f;
    [SerializeField] float hitboxHeight = 0.72f;
    [SerializeField] float lightHitDelay = 0.08f;
    [SerializeField] float heavyHitDelay = 0.12f;
    [SerializeField] float lightKnockback = 0.25f;
    [SerializeField] float heavyKnockback = 1.35f;

    Animator _animator;
    Rigidbody2D _body;
    PlayerMovement _movement;
    readonly Collider2D[] _hits = new Collider2D[12];

    int _comboStep;
    float _comboTimer;
    float _attackLock;
    float _strikeDelay;
    float _pendingKnockback;
    bool _pendingStrike;

    public bool IsAttacking => _attackLock > 0f;

    public void Cancel()
    {
        _attackLock = 0f;
        _pendingStrike = false;
        _comboStep = 0;
        _comboTimer = 0f;
    }

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _body = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (_attackLock > 0f)
            _attackLock -= Time.deltaTime;

        if (_comboTimer > 0f)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
                _comboStep = 0;
        }

        if (!_pendingStrike)
            return;

        _strikeDelay -= Time.deltaTime;
        if (_strikeDelay > 0f)
            return;

        _pendingStrike = false;
        Strike();
    }

    public void TryLight()
    {
        if (_attackLock > comboDelay)
            return;

        int step = _comboTimer <= 0f ? 0 : _comboStep;
        if (step <= 1)
        {
            PlayAttack("Attack1", lightLockTime, lightLunge, lightHitDelay, lightKnockback);
            _comboStep = step + 1;
        }
        else
        {
            PlayAttack("Attack2", finisherLockTime, lightLunge * 1.15f, lightHitDelay, lightKnockback);
            _comboStep = 0;
        }

        _comboTimer = comboResetTime;
    }

    public void TryHeavy()
    {
        if (_attackLock > 0f)
            return;

        _comboStep = 0;
        _comboTimer = 0f;
        PlayAttack("Attack3", heavyLockTime, heavyLunge, heavyHitDelay, heavyKnockback);
    }

    void PlayAttack(string stateName, float lockTime, float lunge, float hitDelay, float knockback)
    {
        _attackLock = lockTime;
        _pendingStrike = true;
        _strikeDelay = hitDelay;
        _pendingKnockback = knockback;

        if (_animator != null)
        {
            _animator.ResetTrigger(Animator.StringToHash("Jump"));
            _animator.ResetTrigger(Animator.StringToHash("Roll"));
            _animator.Play(stateName, 0, 0f);
        }

        if (_body == null)
            return;

        int facing = _movement != null ? _movement.Facing : 1;
        Vector2 velocity = _body.linearVelocity;
        velocity.x = facing * lunge;
        _body.linearVelocity = velocity;
    }

    void Strike()
    {
        int facing = _movement != null ? _movement.Facing : 1;
        Vector2 origin = (Vector2)transform.position + new Vector2(facing * hitboxForward, hitboxHeight);

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;

        int count = Physics2D.OverlapBox(origin, hitboxSize, 0f, filter, _hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _hits[i];
            if (hit == null || hit.gameObject == gameObject)
                continue;

            MushroomEnemy enemy = hit.GetComponent<MushroomEnemy>();
            if (enemy == null)
                enemy = hit.GetComponentInParent<MushroomEnemy>();
            if (enemy == null)
                continue;

            enemy.TakeHit(transform, _pendingKnockback);
        }
    }

    void OnDrawGizmosSelected()
    {
        int facing = _movement != null ? _movement.Facing : 1;
        Vector2 origin = (Vector2)transform.position + new Vector2(facing * hitboxForward, hitboxHeight);
        Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.35f);
        Gizmos.DrawWireCube(origin, hitboxSize);
    }
}
