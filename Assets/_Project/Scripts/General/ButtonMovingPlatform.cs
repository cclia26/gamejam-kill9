using UnityEngine;

public class ButtonMovingPlatform : MonoBehaviour
{
    private enum PositionSpace
    {
        World,
        ParentLocal
    }

    [SerializeField] private PressureButton button;
    [SerializeField] private PositionSpace positionSpace = PositionSpace.ParentLocal;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float moveSpeed = 2f;

    private void Update()
    {
        if (button == null)
        {
            return;
        }

        Vector3 destination = button.pressed ? GetWorldPosition(targetPosition) : GetWorldPosition(startPosition);
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    private Vector3 GetWorldPosition(Vector3 position)
    {
        if (positionSpace == PositionSpace.ParentLocal && transform.parent != null)
        {
            return transform.parent.TransformPoint(position);
        }

        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 startWorldPosition = GetWorldPosition(startPosition);
        Vector3 targetWorldPosition = GetWorldPosition(targetPosition);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startWorldPosition, 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetWorldPosition, 0.15f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startWorldPosition, targetWorldPosition);
    }
}
