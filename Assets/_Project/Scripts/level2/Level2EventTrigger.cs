using UnityEngine;

/// <summary>
/// 通用事件触发器。用于关卡中各种一次性事件（如 first_gap_cleared, bridge_crossed 等）。
/// 放置对应触发区域，配置 eventType 字符串即可。
/// </summary>
public class Level2EventTrigger : MonoBehaviour
{
    [Tooltip("事件类型: first_gap_cleared, bridge_crossed, lift_arrived, sub2_door_enter")]
    [SerializeField] private string eventType = "";

    private BigLevel2DialogueController _controller;
    private bool _fired;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_fired) return;
        _fired = true;
        _controller?.OnEvent(eventType);
    }
}
