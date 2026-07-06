using UnityEngine;

/// <summary>
/// 死亡区域。玩家进入时（掉坑/碰刺）通知 BigLevel2DialogueController。
/// 同时将玩家重生到最近的检查点。
/// </summary>
public class Level2DeathZone : MonoBehaviour
{
    public enum DeathReason { Pit, Spike }

    [SerializeField] private DeathReason deathReason = DeathReason.Pit;

    private BigLevel2DialogueController _controller;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        string reason = deathReason == DeathReason.Spike ? "spike" : "pit";
        _controller?.OnPlayerDeath(reason);
    }
}
