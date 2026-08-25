using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;

    [Header("Wall Slide")]
    [SerializeField] float wallSlideSpeed = 1.6f;
    [SerializeField] float wallJumpHorizontalForce = 6.5f;
    [SerializeField] float wallJumpVerticalForce = 8f;
    [SerializeField] float wallJumpLockTime = 0.18f;
    [SerializeField] float wallSensorDisableTime = 0.2f;

    [Header("Sensors")]
    [SerializeField] PlayerSensor groundSensor;
    [SerializeField] PlayerSensor wallSensorR1;
    [SerializeField] PlayerSensor wallSensorR2;
    [SerializeField] PlayerSensor wallSensorL1;
    [SerializeField] PlayerSensor wallSensorL2;

    [Header("FX")]
    [SerializeField] GameObject slideDust;

    Rigidbody2D _body;
    Animator _animator;
    SpriteRenderer _sprite;
    InputAction _moveAction;
    InputAction _jumpAction;

    bool _grounded;
    bool _wallSliding;
    int _facing = 1;
    int _wallDir;
    int _lastWallDir;
    float _coyoteCounter;
    float _jumpBufferCounter;
    float _wallCoyoteCounter;
    float _wallJumpLockCounter;
    float _idleDelay;

    static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int AirSpeedYHash = Animator.StringToHash("AirSpeedY");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int WallSlideHash = Animator.StringToHash("WallSlide");

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();

        _body.freezeRotation = true;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BindSensors();
        BindInput();
    }

    void Update()
    {
        float moveX = ReadMoveX();
        if (WasJumpPressed())
            _jumpBufferCounter = jumpBufferTime;

        UpdateWallSlide(moveX);
        UpdateGrounded();
        Face(moveX);
        TryJump();
        UpdateAnimator(moveX);
    }

    void FixedUpdate()
    {
        float moveX = ReadMoveX();
        Vector2 velocity = _body.linearVelocity;

        if (_wallJumpLockCounter > 0f)
        {
            _wallJumpLockCounter -= Time.fixedDeltaTime;
        }
        else if (_wallSliding)
        {
            velocity.x = _wallDir * 0.4f;
            velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
        }
        else
        {
            velocity.x = moveX * moveSpeed;
        }

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

    void BindInput()
    {
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
            return;

        _moveAction = actions.FindAction("Player/Move") ?? actions.FindAction("Move");
        _jumpAction = actions.FindAction("Player/Jump") ?? actions.FindAction("Jump");
    }

    float ReadMoveX()
    {
        if (_moveAction != null)
            return _moveAction.ReadValue<Vector2>().x;

        float x = 0f;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                x += 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
            x += gamepad.leftStick.ReadValue().x;

        return Mathf.Clamp(x, -1f, 1f);
    }

    bool WasJumpPressed()
    {
        if (_jumpAction != null)
            return _jumpAction.WasPressedThisFrame();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    void UpdateGrounded()
    {
        bool sensorGrounded = groundSensor != null && groundSensor.IsActive;
        if (sensorGrounded)
            _coyoteCounter = coyoteTime;
        else
            _coyoteCounter -= Time.deltaTime;

        _grounded = sensorGrounded && !_wallSliding;
        _animator.SetBool(GroundedHash, _grounded);
    }

    void UpdateWallSlide(float moveX)
    {
        bool touchingRight = IsTouchingWall(wallSensorR1, wallSensorR2);
        bool touchingLeft = IsTouchingWall(wallSensorL1, wallSensorL2);
        bool falling = _body.linearVelocity.y <= 0.05f;
        bool inAir = groundSensor == null || !groundSensor.IsActive;

        int wallDir = 0;
        if (touchingRight)
            wallDir = 1;
        else if (touchingLeft)
            wallDir = -1;

        if (wallDir != 0 && inAir)
        {
            _lastWallDir = wallDir;
            _wallCoyoteCounter = coyoteTime;
        }
        else
        {
            _wallCoyoteCounter -= Time.deltaTime;
        }

        bool pushingAway = wallDir != 0 && moveX * wallDir < -0.1f;
        _wallSliding = inAir && falling && wallDir != 0 && !pushingAway && _wallJumpLockCounter <= 0f;
        _wallDir = _wallSliding ? wallDir : 0;

        _animator.SetBool(WallSlideHash, _wallSliding);
    }

    static bool IsTouchingWall(PlayerSensor upper, PlayerSensor lower)
    {
        bool upperActive = upper != null && upper.IsActive;
        bool lowerActive = lower != null && lower.IsActive;
        return upperActive && lowerActive;
    }

    void Face(float moveX)
    {
        if (_wallSliding)
        {
            _facing = _wallDir;
            _sprite.flipX = _facing < 0;
            return;
        }

        if (Mathf.Abs(moveX) < 0.01f || _wallJumpLockCounter > 0f)
            return;

        _facing = moveX > 0f ? 1 : -1;
        _sprite.flipX = _facing < 0;
    }

    void TryJump()
    {
        _jumpBufferCounter -= Time.deltaTime;
        if (_jumpBufferCounter <= 0f)
            return;

        if (CanWallJump())
        {
            int jumpDir = -_lastWallDir;
            _body.linearVelocity = new Vector2(jumpDir * wallJumpHorizontalForce, wallJumpVerticalForce);
            _facing = jumpDir;
            _sprite.flipX = _facing < 0;
            _wallSliding = false;
            _wallJumpLockCounter = wallJumpLockTime;
            _jumpBufferCounter = 0f;
            _coyoteCounter = 0f;
            _wallCoyoteCounter = 0f;

            DisableWallSensors();
            _animator.SetBool(WallSlideHash, false);
            _animator.SetBool(GroundedHash, false);
            _animator.SetTrigger(JumpHash);
            return;
        }

        if (_coyoteCounter <= 0f)
            return;

        _body.linearVelocity = new Vector2(_body.linearVelocity.x, jumpForce);
        _grounded = false;
        _coyoteCounter = 0f;
        _jumpBufferCounter = 0f;
        groundSensor?.Disable(0.15f);
        _animator.SetBool(GroundedHash, false);
        _animator.SetTrigger(JumpHash);
    }

    bool CanWallJump()
    {
        if (_wallJumpLockCounter > 0f)
            return false;
        if (_grounded)
            return false;
        if (!_wallSliding && _coyoteCounter > 0f)
            return false;
        if (_wallCoyoteCounter <= 0f || _lastWallDir == 0)
            return false;

        return true;
    }

    void DisableWallSensors()
    {
        wallSensorR1?.Disable(wallSensorDisableTime);
        wallSensorR2?.Disable(wallSensorDisableTime);
        wallSensorL1?.Disable(wallSensorDisableTime);
        wallSensorL2?.Disable(wallSensorDisableTime);
    }

    void UpdateAnimator(float moveX)
    {
        _animator.SetFloat(AirSpeedYHash, _body.linearVelocity.y);

        if (_wallSliding || !_grounded)
            return;

        if (Mathf.Abs(moveX) > 0.01f)
        {
            _idleDelay = 0.05f;
            _animator.SetInteger(AnimStateHash, 1);
            return;
        }

        _idleDelay -= Time.deltaTime;
        if (_idleDelay < 0f)
            _animator.SetInteger(AnimStateHash, 0);
    }

    // Called from HeroKnight_WallSlide animation event.
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
