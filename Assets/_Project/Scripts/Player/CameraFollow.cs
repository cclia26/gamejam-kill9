using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Collider2D cameraBounds;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        ResolveTargetIfMissing();
    }

    private void ResolveTargetIfMissing()
    {
        if (target != null)
        {
            return;
        }

        var homePlayer = FindObjectOfType<Controller_Home>();
        if (homePlayer != null)
        {
            target = homePlayer.transform;
            return;
        }

        var empathyPlayer = FindObjectOfType<Controller_Empathy>();
        if (empathyPlayer != null)
        {
            target = empathyPlayer.transform;
            return;
        }

        var willPlayer = FindObjectOfType<Controller_Will>();
        if (willPlayer != null)
        {
            target = willPlayer.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            target = taggedPlayer.transform;
            return;
        }

        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            target = namedPlayer.transform;
        }
    }

    public void SetBounds(Collider2D newBounds)
    {
        cameraBounds = newBounds;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            ResolveTargetIfMissing();
        }

        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetCameraPosition();

        transform.position = ClampToBounds(targetPosition);
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            ResolveTargetIfMissing();
        }

        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetCameraPosition();

        targetPosition = ClampToBounds(targetPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    private Vector3 GetTargetCameraPosition()
    {
        return new Vector3(
            followX ? target.position.x : transform.position.x,
            followY ? target.position.y : transform.position.y,
            transform.position.z
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
