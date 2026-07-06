using UnityEngine;

/// <summary>
/// 最终门触发器。玩家进入最终门时启动 Win 流程。
/// 放在 SubSection 3 最终门后面。
/// </summary>
public class Level2FinalDoorTrigger : MonoBehaviour
{
    private BigLevel2DialogueController _controller;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _controller?.OnFinalDoorEntered();
    }
}
