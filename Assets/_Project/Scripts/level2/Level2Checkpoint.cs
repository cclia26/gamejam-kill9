using UnityEngine;

/// <summary>
/// 检查点。玩家进入时更新重生位置并重置当前检查点死亡计数。
/// </summary>
public class Level2Checkpoint : MonoBehaviour
{
    [SerializeField] private Sprite activeSprite;

    private BigLevel2DialogueController _controller;
    private SpriteRenderer _spriteRenderer;
    private bool _activated;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_activated) return;

        _activated = true;
        _controller?.OnCheckpointReached(transform.position);

        if (_spriteRenderer != null && activeSprite != null)
            _spriteRenderer.sprite = activeSprite;
    }
}
