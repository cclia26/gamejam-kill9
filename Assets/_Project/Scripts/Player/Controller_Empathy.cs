using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Controller_Empathy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCheckHeight = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private float moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null)
        {
            playerCollider = GetComponentInChildren<Collider2D>();
        }
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        if (jumpRequested && IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        jumpRequested = false;
    }

    private bool IsGrounded()
    {
        if (playerCollider == null)
        {
            Debug.LogWarning("Controller_Empathy needs a Collider2D on the player or its children.", this);
            return false;
        }

        Bounds bounds = playerCollider.bounds;
        Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y - groundCheckHeight * 0.5f);
        Vector2 checkSize = new Vector2(bounds.size.x * 0.9f, groundCheckHeight);

        return Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundLayer);
    }
}
