using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Controller_Home : MonoBehaviour
{
    private enum FacingDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private FacingDirection facingDirection = FacingDirection.Front;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        UpdateFacingDirection(x, y);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateFacingDirection(float x, float y)
    {
        if (y > 0)
        {
            facingDirection = FacingDirection.Back;
        }
        else if (y < 0)
        {
            facingDirection = FacingDirection.Front;
        }
        else if (x < 0)
        {
            facingDirection = FacingDirection.Left;
        }
        else if (x > 0)
        {
            facingDirection = FacingDirection.Right;
        }
    }
}
