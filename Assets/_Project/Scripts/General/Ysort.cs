using UnityEngine;

public class Ysort : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D sortCollider;

    private const int SortingPrecision = 10;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float sortY = sortCollider != null ? sortCollider.bounds.min.y : transform.position.y;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-sortY * SortingPrecision);
    }
}
