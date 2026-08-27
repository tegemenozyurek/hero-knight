using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(0f, 1.2f);
    [SerializeField] float smoothTime = 0.18f;
    [SerializeField] float lookAheadDistance = 1.6f;
    [SerializeField] float lookAheadSmoothTime = 0.35f;

    [Header("Bounds (optional)")]
    [SerializeField] bool useBounds;
    [SerializeField] Vector2 minBounds = new Vector2(-20f, -10f);
    [SerializeField] Vector2 maxBounds = new Vector2(20f, 10f);

    Vector3 _velocity;
    float _lookAhead;
    float _lookAheadVelocity;
    float _cameraZ;
    SpriteRenderer _targetSprite;

    void Awake()
    {
        _cameraZ = transform.position.z;
        ResolveTarget();
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (!IsSceneObject(target))
            ResolveTarget();

        if (target == null)
            return;

        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref _velocity, smoothTime);
    }

    void ResolveTarget()
    {
        if (IsSceneObject(target))
        {
            CacheSprite();
            return;
        }

        target = null;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            target = player.transform;
        else
        {
            GameObject found = GameObject.Find("Player");
            if (found != null && IsSceneObject(found.transform))
                target = found.transform;
        }

        CacheSprite();
    }

    void CacheSprite()
    {
        _targetSprite = target != null ? target.GetComponent<SpriteRenderer>() : null;
    }

    void SnapToTarget()
    {
        if (target == null)
            return;

        _lookAhead = 0f;
        _lookAheadVelocity = 0f;
        _velocity = Vector3.zero;
        transform.position = DesiredPosition();
    }

    Vector3 DesiredPosition()
    {
        float facing = 1f;
        if (_targetSprite != null)
            facing = _targetSprite.flipX ? -1f : 1f;

        float lookAheadTarget = facing * lookAheadDistance;
        _lookAhead = Mathf.SmoothDamp(_lookAhead, lookAheadTarget, ref _lookAheadVelocity, lookAheadSmoothTime);

        Vector3 desired = new Vector3(
            target.position.x + offset.x + _lookAhead,
            target.position.y + offset.y,
            _cameraZ);

        if (useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
        }

        return desired;
    }

    static bool IsSceneObject(Transform t)
    {
        return t != null && t.gameObject.scene.IsValid();
    }
}
