using UnityEngine;
using UnityEngine.Events;

public class level2Victory : MonoBehaviour
{
    [SerializeField] private DoorLock requiredDoor;
    [SerializeField] private MonoBehaviour[] playerMovementComponents;
    [SerializeField] private UnityEvent onVictory;

    private bool victoryTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (victoryTriggered || !other.CompareTag("Player") || !IsDoorOpen())
        {
            return;
        }

        victoryTriggered = true;

        // 这里锁住玩家控制，胜利后的流程从 onVictory 事件触发继续扩展。
        DisablePlayerMovement(other);
        // 后续可在事件里接对白、UI 或切场景。
        onVictory?.Invoke();
    }

    private bool IsDoorOpen()
    {
        return requiredDoor == null || requiredDoor.open;
    }

    private void DisablePlayerMovement(Collider2D playerCollider)
    {
        foreach (MonoBehaviour movementComponent in playerMovementComponents)
        {
            if (movementComponent != null)
            {
                movementComponent.enabled = false;
            }
        }

        Rigidbody2D rb = playerCollider.attachedRigidbody;
        if (rb == null)
        {
            rb = playerCollider.GetComponentInParent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
}
