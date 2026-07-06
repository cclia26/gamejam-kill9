using UnityEngine;

/// <summary>
/// 靠近触发器。放置于按钮/门旁，用于检测玩家靠近以触发空闲台词。
/// </summary>
public class Level2ProximityTrigger : MonoBehaviour
{
    public enum ProximityType { Button, Door, FinalDoor }

    [SerializeField] private ProximityType proximityType = ProximityType.Button;

    private BigLevel2DialogueController _controller;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _controller?.OnProximityEnter(proximityType.ToString().ToLowerInvariant());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _controller?.OnProximityExit(proximityType.ToString().ToLowerInvariant());
    }
}
