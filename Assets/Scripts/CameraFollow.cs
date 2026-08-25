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

        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                target = player.transform;
        }

        if (target != null)
            _targetSprite = target.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

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

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }
}
