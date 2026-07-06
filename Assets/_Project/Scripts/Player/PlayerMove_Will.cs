using UnityEngine;

public class PlayerMove_Will : MonoBehaviour
{
    private const float RespawnViewportX = 0.5f;
    private const float RespawnViewportY = 2f / 3f;

    [SerializeField] private float playerFallSpeed = 8f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Respawn();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(0, -playerFallSpeed);
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        float distanceFromCamera = transform.position.z - camera.transform.position.z;
        float leftBoundaryX = camera.ViewportToWorldPoint(new Vector3(0f, 0.5f, distanceFromCamera)).x;

        if (transform.position.x < leftBoundaryX)
        {
            Respawn(camera);
        }
    }

    private void Respawn()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        Respawn(camera);
    }

    private void Respawn(Camera camera)
    {
        float distanceFromCamera = transform.position.z - camera.transform.position.z;
        Vector3 respawnPosition = camera.ViewportToWorldPoint(new Vector3(RespawnViewportX, RespawnViewportY, distanceFromCamera));

        rb.velocity = Vector2.zero;
        rb.position = respawnPosition;
    }
}
