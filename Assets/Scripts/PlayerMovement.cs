using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Walk")]
    [SerializeField] float walkSpeed = 4.4f;
    [SerializeField] float groundAccel = 36f;
    [SerializeField] float groundDecel = 54f;
    [SerializeField] float turnDecel = 72f;
    [SerializeField] float airAccel = 9f;
    [SerializeField] float airDecel = 6f;
    [SerializeField] float inputDeadzone = 0.2f;

    [Header("Jump")]
    [SerializeField] float jumpSpeed = 13.2f;
    [SerializeField] float gravityScale = 3.5f;
    [SerializeField] float fallMultiplier = 1.4f;
    [SerializeField] float maxFallSpeed = 16f;
    [SerializeField] float coyoteTime = 0.09f;
    [SerializeField] float jumpBufferTime = 0.12f;

    [Header("Wall")]
    [SerializeField] float wallSlideSpeed = 2.4f;
    [SerializeField] float wallJumpSpeedX = 5.6f;
    [SerializeField] float wallJumpSpeedY = 9.2f;
    [SerializeField] float wallJumpLockTime = 0.1f;
    [SerializeField] float wallSensorDisableTime = 0.1f;

    [Header("Roll")]
    [SerializeField] float rollSpeed = 8.2f;
    [SerializeField] float rollEndSpeed = 3.4f;
    [SerializeField] float rollDuration = 0.62f;
    [SerializeField] float rollCooldown = 0.18f;
    [SerializeField] float rollColliderHeight = 0.48f;

    [Header("Sensors")]
    [SerializeField] PlayerSensor groundSensor;
    [SerializeField] PlayerSensor wallSensorR1;
    [SerializeField] PlayerSensor wallSensorR2;
    [SerializeField] PlayerSensor wallSensorL1;
    [SerializeField] PlayerSensor wallSensorL2;

    [Header("FX")]
    [SerializeField] GameObject slideDust;

    public bool IsGrounded => _grounded;
    public bool IsRolling => _rolling;
    public bool IsWallSliding => _wallSliding;
    public int Facing => _facing;
    public bool IsBusy => _rolling || (_attack != null && _attack.IsAttacking);
    public bool CanStartAttack => !_rolling && !_wallSliding && (_grounded || _coyote > 0f);

    Rigidbody2D _body;
    Animator _animator;
    SpriteRenderer _sprite;
    BoxCollider2D _box;
    InputAction _moveAction;
    InputAction _jumpAction;
    InputAction _rollAction;
    InputAction _lightAttackAction;
    InputAction _heavyAttackAction;

    Vector2 _standSize;
    Vector2 _standOffset;
    readonly Collider2D[] _overlapHits = new Collider2D[8];

    float _inputX;
    float _jumpBuffer;
    float _coyote;
    float _wallCoyote;
    float _wallJumpLock;
    float _rollTime;
    float _rollCooldown;
    int _facing = 1;
    int _wallDir;
    int _lastWallDir;
    int _rollDir = 1;
    bool _grounded;
    bool _wallSliding;
    bool _rolling;
    float _idleDelay;
    PlayerAttack _attack;

    static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int AirSpeedYHash = Animator.StringToHash("AirSpeedY");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int WallSlideHash = Animator.StringToHash("WallSlide");
    static readonly int RollHash = Animator.StringToHash("Roll");

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        _box = GetComponent<BoxCollider2D>();

        _body.freezeRotation = true;
        _body.gravityScale = gravityScale;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        _body.linearDamping = 0f;

        if (_box != null)
        {
            _standSize = _box.size;
            _standOffset = _box.offset;
        }

        PhysicsMaterial2D material = new PhysicsMaterial2D("PlayerNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        _body.sharedMaterial = material;
        if (_box != null)
            _box.sharedMaterial = material;

        BindSensors();
        BuildInput();
        _attack = GetComponent<PlayerAttack>();
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _jumpAction?.Enable();
        _rollAction?.Enable();
        _lightAttackAction?.Enable();
        _heavyAttackAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _jumpAction?.Disable();
        _rollAction?.Disable();
        _lightAttackAction?.Disable();
        _heavyAttackAction?.Disable();
    }

    void OnDestroy()
    {
        _moveAction?.Dispose();
        _jumpAction?.Dispose();
        _rollAction?.Dispose();
        _lightAttackAction?.Dispose();
        _heavyAttackAction?.Dispose();
    }

    void Update()
    {
        ReadInput();
        UpdateGrounded();
        UpdateWallSlide();
        TryStartRoll();
        TryStartAttack();
        UpdateFacing();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        Vector2 velocity = _body.linearVelocity;

        if (_wallJumpLock > 0f)
            _wallJumpLock -= Time.fixedDeltaTime;
        if (_rollCooldown > 0f)
            _rollCooldown -= Time.fixedDeltaTime;

        Move(ref velocity);
        ApplyGravity(ref velocity);
        TryJump(ref velocity);
        if (!_rolling)
            TryStandUp();

        _body.linearVelocity = velocity;
    }

    void BindSensors()
    {
        if (groundSensor == null)
            groundSensor = FindSensor("Ground");
        if (wallSensorR1 == null)
            wallSensorR1 = FindSensor("WallSensor_R1");
        if (wallSensorR2 == null)
            wallSensorR2 = FindSensor("WallSensor_R2");
        if (wallSensorL1 == null)
            wallSensorL1 = FindSensor("WallSensor_L1");
        if (wallSensorL2 == null)
            wallSensorL2 = FindSensor("WallSensor_L2");
    }

    PlayerSensor FindSensor(string sensorName)
    {
        Transform child = transform.Find(sensorName);
        return child != null ? child.GetComponent<PlayerSensor>() : null;
    }

    void BuildInput()
    {
        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s");
        _moveAction.AddBinding("<Gamepad>/leftStick");

        _jumpAction = new InputAction("Jump", InputActionType.Button);
        _jumpAction.AddBinding("<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");

        _rollAction = new InputAction("Roll", InputActionType.Button);
        _rollAction.AddBinding("<Keyboard>/leftShift");
        _rollAction.AddBinding("<Keyboard>/rightShift");
        _rollAction.AddBinding("<Gamepad>/buttonEast");

        _lightAttackAction = new InputAction("LightAttack", InputActionType.Button);
        _lightAttackAction.AddBinding("<Keyboard>/k");
        _lightAttackAction.AddBinding("<Keyboard>/j");
        _lightAttackAction.AddBinding("<Keyboard>/f");
        _lightAttackAction.AddBinding("<Mouse>/leftButton");
        _lightAttackAction.AddBinding("<Gamepad>/buttonWest");

        _heavyAttackAction = new InputAction("HeavyAttack", InputActionType.Button);
        _heavyAttackAction.AddBinding("<Keyboard>/l");
        _heavyAttackAction.AddBinding("<Mouse>/rightButton");
        _heavyAttackAction.AddBinding("<Gamepad>/buttonNorth");
    }

    void ReadInput()
    {
        float x = 0f;

        if (_moveAction != null)
            x = _moveAction.ReadValue<Vector2>().x;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            if (left && !right)
                x = -1f;
            else if (right && !left)
                x = 1f;
            else if (left && right)
                x = 0f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            float stickX = gamepad.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) > Mathf.Abs(x))
                x = stickX;
            if (gamepad.dpad.left.isPressed)
                x = -1f;
            if (gamepad.dpad.right.isPressed)
                x = 1f;
        }

        _inputX = Mathf.Abs(x) < inputDeadzone ? 0f : Mathf.Clamp(x, -1f, 1f);

        if (WasJumpPressed())
            _jumpBuffer = jumpBufferTime;
        else
            _jumpBuffer -= Time.deltaTime;
    }

    bool WasJumpPressed()
    {
        if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            return true;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    bool WasRollPressed()
    {
        if (_rollAction != null && _rollAction.WasPressedThisFrame())
            return true;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame))
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
    }

    bool WasLightAttackPressed()
    {
        if (_lightAttackAction != null && _lightAttackAction.WasPressedThisFrame())
            return true;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.kKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
    }

    bool WasHeavyAttackPressed()
    {
        if (_heavyAttackAction != null && _heavyAttackAction.WasPressedThisFrame())
            return true;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
    }

    void TryStartAttack()
    {
        if (_attack == null)
            _attack = GetComponent<PlayerAttack>();
        if (_attack == null)
            return;
        if (_rolling || _wallSliding)
            return;

        if (WasLightAttackPressed())
            _attack.TryLight();
        else if (WasHeavyAttackPressed())
            _attack.TryHeavy();
    }

    void TryStartRoll()
    {
        if (!WasRollPressed())
            return;
        if (_rolling || _rollCooldown > 0f)
            return;
        if (_attack != null && _attack.IsAttacking)
            return;
        if (!_grounded || _wallSliding)
            return;

        _rollDir = Mathf.Abs(_inputX) > 0.01f ? (_inputX > 0f ? 1 : -1) : _facing;
        _facing = _rollDir;
        _sprite.flipX = _facing < 0;
        _rolling = true;
        _rollTime = rollDuration;
        _rollCooldown = rollDuration + rollCooldown;
        _jumpBuffer = 0f;
        SetCrouching(true);
        _animator.ResetTrigger(JumpHash);
        _animator.SetTrigger(RollHash);
    }

    void Move(ref Vector2 velocity)
    {
        if (_rolling)
        {
            _rollTime -= Time.fixedDeltaTime;
            float t = rollDuration <= 0f ? 0f : Mathf.Clamp01(_rollTime / rollDuration);
            velocity.x = _rollDir * Mathf.Lerp(rollEndSpeed, rollSpeed, t);
            if (_rollTime <= 0f)
                _rolling = false;
            return;
        }

        if (_attack != null && _attack.IsAttacking)
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, groundDecel * 0.7f * Time.fixedDeltaTime);
            return;
        }

        if (_wallJumpLock > 0f)
            return;

        if (_wallSliding)
        {
            velocity.x = _wallDir * 0.35f;
            velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
            return;
        }

        float target = _inputX * walkSpeed;
        float rate;

        if (_grounded)
        {
            bool reversing = Mathf.Abs(_inputX) > 0.01f &&
                             Mathf.Abs(velocity.x) > 0.25f &&
                             Mathf.Sign(_inputX) != Mathf.Sign(velocity.x);
            if (reversing)
                rate = turnDecel;
            else if (Mathf.Abs(target) > 0.01f)
                rate = groundAccel;
            else
                rate = groundDecel;
        }
        else
        {
            rate = Mathf.Abs(target) > 0.01f ? airAccel : airDecel;
        }

        velocity.x = Mathf.MoveTowards(velocity.x, target, rate * Time.fixedDeltaTime);
    }

    void ApplyGravity(ref Vector2 velocity)
    {
        if (_wallSliding)
            return;

        // Committed jump: full arc, slightly heavier on the way down.
        if (velocity.y < 0f)
        {
            velocity.y += Physics2D.gravity.y * _body.gravityScale * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            if (velocity.y < -maxFallSpeed)
                velocity.y = -maxFallSpeed;
        }
    }

    void TryJump(ref Vector2 velocity)
    {
        if (_jumpBuffer <= 0f)
            return;
        if (_rolling)
            return;
        if (_attack != null && _attack.IsAttacking)
            return;
        if (IsCrouching() && !CanStand())
            return;

        if (CanWallJump())
        {
            int jumpDir = -_lastWallDir;
            velocity = new Vector2(jumpDir * wallJumpSpeedX, wallJumpSpeedY);
            _facing = jumpDir;
            _sprite.flipX = _facing < 0;
            _wallSliding = false;
            _wallJumpLock = wallJumpLockTime;
            _jumpBuffer = 0f;
            _coyote = 0f;
            _wallCoyote = 0f;
            DisableWallSensors();
            _animator.SetBool(WallSlideHash, false);
            _animator.SetBool(GroundedHash, false);
            _animator.SetTrigger(JumpHash);
            return;
        }

        if (_coyote <= 0f)
            return;

        velocity.y = jumpSpeed;
        _grounded = false;
        _coyote = 0f;
        _jumpBuffer = 0f;
        groundSensor?.Disable(0.12f);
        _animator.SetBool(GroundedHash, false);
        _animator.SetTrigger(JumpHash);
    }

    bool CanWallJump()
    {
        if (_wallJumpLock > 0f)
            return false;
        if (_grounded)
            return false;
        if (!_wallSliding && _coyote > 0f)
            return false;
        return _wallCoyote > 0f && _lastWallDir != 0;
    }

    void UpdateGrounded()
    {
        bool onGround = groundSensor != null && groundSensor.IsActive;
        if (onGround)
            _coyote = coyoteTime;
        else
            _coyote -= Time.deltaTime;

        _grounded = onGround && !_wallSliding;
        _animator.SetBool(GroundedHash, _grounded);
    }

    void UpdateWallSlide()
    {
        bool right = IsTouchingWall(wallSensorR1, wallSensorR2);
        bool left = IsTouchingWall(wallSensorL1, wallSensorL2);
        bool falling = _body.linearVelocity.y <= 0.08f;
        bool inAir = groundSensor == null || !groundSensor.IsActive;

        int wallDir = 0;
        if (right)
            wallDir = 1;
        else if (left)
            wallDir = -1;

        if (wallDir != 0 && inAir)
        {
            _lastWallDir = wallDir;
            _wallCoyote = coyoteTime;
        }
        else
        {
            _wallCoyote -= Time.deltaTime;
        }

        bool holdingIntoWall = wallDir != 0 && _inputX * wallDir > 0.1f;
        _wallSliding = !_rolling && inAir && falling && holdingIntoWall && _wallJumpLock <= 0f;
        _wallDir = _wallSliding ? wallDir : 0;
        _animator.SetBool(WallSlideHash, _wallSliding);
    }

    static bool IsTouchingWall(PlayerSensor a, PlayerSensor b)
    {
        return a != null && b != null && a.IsActive && b.IsActive;
    }

    void UpdateFacing()
    {
        if (_rolling)
            return;
        if (_attack != null && _attack.IsAttacking)
            return;

        if (_wallSliding)
        {
            _facing = _wallDir;
            _sprite.flipX = _facing < 0;
            return;
        }

        if (_wallJumpLock > 0f || Mathf.Abs(_inputX) < 0.01f)
            return;

        _facing = _inputX > 0f ? 1 : -1;
        _sprite.flipX = _facing < 0;
    }

    void SetCrouching(bool crouch)
    {
        if (_box == null)
            return;

        if (!crouch)
        {
            _box.size = _standSize;
            _box.offset = _standOffset;
            return;
        }

        float bottom = _standOffset.y - _standSize.y * 0.5f;
        float height = Mathf.Min(rollColliderHeight, _standSize.y);
        _box.size = new Vector2(_standSize.x, height);
        _box.offset = new Vector2(_standOffset.x, bottom + height * 0.5f);
    }

    bool IsCrouching()
    {
        return _box != null && _box.size.y + 0.001f < _standSize.y;
    }

    bool CanStand()
    {
        if (_box == null)
            return true;

        float standTop = _standOffset.y + _standSize.y * 0.5f;
        float crouchTop = (_standOffset.y - _standSize.y * 0.5f) + Mathf.Min(rollColliderHeight, _standSize.y);
        float headHeight = standTop - crouchTop;
        if (headHeight <= 0.01f)
            return true;

        Vector2 worldCenter = (Vector2)transform.position + new Vector2(_standOffset.x, crouchTop + headHeight * 0.5f);
        Vector2 headSize = new Vector2(_standSize.x * 0.9f, headHeight);

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;

        int count = Physics2D.OverlapBox(worldCenter, headSize, 0f, filter, _overlapHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _overlapHits[i];
            if (hit == null || hit == _box)
                continue;
            if (hit.attachedRigidbody == _body)
                continue;
            return false;
        }

        return true;
    }

    void TryStandUp()
    {
        if (!IsCrouching())
            return;
        if (!CanStand())
            return;

        SetCrouching(false);
    }

    void DisableWallSensors()
    {
        wallSensorR1?.Disable(wallSensorDisableTime);
        wallSensorR2?.Disable(wallSensorDisableTime);
        wallSensorL1?.Disable(wallSensorDisableTime);
        wallSensorL2?.Disable(wallSensorDisableTime);
    }

    void UpdateAnimator()
    {
        _animator.SetFloat(AirSpeedYHash, _body.linearVelocity.y);

        if (_rolling || _wallSliding || !_grounded)
            return;
        if (_attack != null && _attack.IsAttacking)
            return;

        if (Mathf.Abs(_inputX) > 0.01f)
        {
            _idleDelay = 0.05f;
            _animator.SetInteger(AnimStateHash, 1);
            return;
        }

        _idleDelay -= Time.deltaTime;
        if (_idleDelay < 0f)
            _animator.SetInteger(AnimStateHash, 0);
    }

    void AE_SlideDust()
    {
        if (slideDust == null || !_wallSliding)
            return;

        Vector3 spawnPosition = transform.position;
        if (_wallDir > 0 && wallSensorR2 != null)
            spawnPosition = wallSensorR2.transform.position;
        else if (_wallDir < 0 && wallSensorL2 != null)
            spawnPosition = wallSensorL2.transform.position;

        GameObject dust = Instantiate(slideDust, spawnPosition, transform.rotation);
        dust.transform.localScale = new Vector3(_facing, 1f, 1f);
    }
}
