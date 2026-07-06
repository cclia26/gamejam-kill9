using UnityEngine;

public class Level1_PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastMoveDirection = Vector2.down;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        lastMoveDirection = direction.normalized;

        if (spriteRenderer != null && Mathf.Abs(lastMoveDirection.x) > 0f)
        {
            spriteRenderer.flipX = lastMoveDirection.x > 0f;
        }

        ApplyAnimatorValues(Vector2.zero);
    }

    private void Update()
    {
        Vector2 moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (moveInput.sqrMagnitude > 0f)
        {
            lastMoveDirection = moveInput;

            if (spriteRenderer != null && Mathf.Abs(moveInput.x) > 0f)
            {
                spriteRenderer.flipX = moveInput.x > 0f;
            }
        }

        ApplyAnimatorValues(moveInput);
    }

    private void ApplyAnimatorValues(Vector2 moveInput)
    {
        animator.SetFloat(MoveX, moveInput.x);
        animator.SetFloat(MoveY, moveInput.y);
        animator.SetFloat(Speed, moveInput.magnitude);
        animator.SetFloat(LastMoveX, lastMoveDirection.x);
        animator.SetFloat(LastMoveY, lastMoveDirection.y);
    }
}
