using UnityEngine;

/// <summary>
/// 小段进入触发器。放置在各小段起始位置，当玩家进入时通知 BigLevel2DialogueController。
/// </summary>
public class Level2SubSectionTrigger : MonoBehaviour
{
    [SerializeField] private int subSection = 1;

    private BigLevel2DialogueController _controller;

    private void Start()
    {
        _controller = FindObjectOfType<BigLevel2DialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _controller?.OnEnterSubSection(subSection);
    }
}
