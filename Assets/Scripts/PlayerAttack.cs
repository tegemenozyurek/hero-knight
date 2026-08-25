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

    Animator _animator;
    Rigidbody2D _body;
    PlayerMovement _movement;

    int _comboStep;
    float _comboTimer;
    float _attackLock;

    public bool IsAttacking => _attackLock > 0f;

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
    }

    public void TryLight()
    {
        if (_attackLock > comboDelay)
            return;

        int step = _comboTimer <= 0f ? 0 : _comboStep;
        if (step <= 1)
        {
            PlayAttack("Attack1", lightLockTime, lightLunge);
            _comboStep = step + 1;
        }
        else
        {
            PlayAttack("Attack2", finisherLockTime, lightLunge * 1.15f);
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
        PlayAttack("Attack3", heavyLockTime, heavyLunge);
    }

    void PlayAttack(string stateName, float lockTime, float lunge)
    {
        _attackLock = lockTime;

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
}
