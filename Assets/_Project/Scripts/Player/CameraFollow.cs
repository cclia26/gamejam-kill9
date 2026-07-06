using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Collider2D cameraBounds;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetBounds(Collider2D newBounds)
    {
        cameraBounds = newBounds;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        transform.position = ClampToBounds(targetPosition);
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        targetPosition = ClampToBounds(targetPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    private Vector3 ClampToBounds(Vector3 targetPosition)
    {
        if (cameraBounds == null || cam == null || !cam.orthographic)
        {
            return targetPosition;
        }

        Bounds bounds = cameraBounds.bounds;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        targetPosition.x = minX > maxX
            ? bounds.center.x
            : Mathf.Clamp(targetPosition.x, minX, maxX);

        targetPosition.y = minY > maxY
            ? bounds.center.y
            : Mathf.Clamp(targetPosition.y, minY, maxY);

        return targetPosition;
    }
}
