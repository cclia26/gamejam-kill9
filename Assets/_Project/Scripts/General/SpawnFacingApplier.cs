using UnityEngine;

public class SpawnFacingApplier : MonoBehaviour
{
    [SerializeField] private Level1_PlayerAnimator playerAnimator;

    private void Awake()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Level1_PlayerAnimator>();
        }
    }

    private void Start()
    {
        ScenePayload payload = GameManager.Instance?.PendingPayload;
        if (payload == null)
        {
            return;
        }

        float facingX = payload.GetExtra<float>(SceneTransition.SpawnFacingXKey);
        float facingY = payload.GetExtra<float>(SceneTransition.SpawnFacingYKey);
        Vector2 facingDirection = new Vector2(facingX, facingY);

        if (facingDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        playerAnimator?.SetFacingDirection(facingDirection);
    }
}
