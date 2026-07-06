using UnityEngine;

public class Platformer_PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGround = Animator.StringToHash("IsGround");
    private static readonly int Jump = Animator.StringToHash("Jump");

    [SerializeField] private float groundCheckHeight = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null)
        {
            playerCollider = GetComponentInChildren<Collider2D>();
        }
    }

    private void Update()
    {
        float moveX = rb.velocity.x;
        bool isGrounded = CheckGrounded();

        if (spriteRenderer != null && Mathf.Abs(moveX) > 0.01f)
        {
            spriteRenderer.flipX = moveX > 0f;
        }

        animator.SetFloat(MoveX, moveX);
        animator.SetFloat(Speed, Mathf.Abs(moveX));
        animator.SetBool(IsGround, isGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            animator.SetTrigger(Jump);
        }
    }

    private bool CheckGrounded()
    {
        if (playerCollider == null)
        {
            return false;
        }

        Bounds bounds = playerCollider.bounds;
        Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y - groundCheckHeight * 0.5f);
        Vector2 checkSize = new Vector2(bounds.size.x * 0.9f, groundCheckHeight);

        return Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundLayer);
    }
}
