using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 对话管理器 — 加载 JSON 对话数据，按条件匹配、排队、逐条播放。
/// 挂载到 GameManager 所在的 GameObject 上。
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [SerializeField] private string jsonResourcePath = "Data/dialogues";

    private List<DialogueLine> _allDialogues = new List<DialogueLine>();
    private HashSet<string> _playedIds = new HashSet<string>();
    private Queue<DialogueLine> _pendingQueue = new Queue<DialogueLine>();
    private bool _isShowing;

    public bool HasPending => _pendingQueue.Count > 0 || _isShowing;

    private void Awake()
    {
        LoadDialogues();
    }

    private void LoadDialogues()
    {
        var asset = Resources.Load<TextAsset>(jsonResourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"DialogueManager: 未找到对话数据 {jsonResourcePath}");
            return;
        }

        var container = JsonUtility.FromJson<DialogueDataContainer>(asset.text);
        if (container?.dialogues != null)
            _allDialogues = container.dialogues;

        Debug.Log($"DialogueManager: 加载 {_allDialogues.Count} 条对话");
    }

    /// <summary>
    /// 根据当前场景触发匹配的对话，加入播放队列。
    /// </summary>
    public void TriggerDialogues(string sceneName)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        var matches = _allDialogues
            .Where(d => !_playedIds.Contains(d.id))
            .Where(d => d.triggerScene == sceneName || string.IsNullOrEmpty(d.triggerScene))
            .Where(d => string.IsNullOrEmpty(d.triggerCondition) || state.GetFlag(d.triggerCondition))
            .Where(d => d.triggerEnterCount < 0 || state.enterCount >= d.triggerEnterCount)
            .OrderByDescending(d => d.priority)
            .ToList();

        foreach (var d in matches)
        {
            _pendingQueue.Enqueue(d);
            if (d.playOnce)
                _playedIds.Add(d.id);
        }

        if (!_isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isShowing = true;
        var display = FindActiveDisplay();

        while (_pendingQueue.Count > 0)
        {
            var d = _pendingQueue.Dequeue();
            yield return ShowDialogue(d, display);
        }

        _isShowing = false;
    }

    private IEnumerator ShowDialogue(DialogueLine d, DialogueDisplay display)
    {
        var state = GameManager.Instance?.State;

        // 构建显示文本
        string displayText;
        if (string.IsNullOrEmpty(d.speaker))
            displayText = d.text;
        else
            displayText = $"{d.speaker}: \"{d.text}\"";

        float speed = 0.04f * d.delay;

        if (display != null)
        {
            yield return display.PlayText(displayText, speed);
        }
        else
        {
            Debug.Log($"[Dialogue] {displayText}");
            yield return new WaitForSeconds(1f);
        }

        // 完成回调：设置 flag
        if (!string.IsNullOrEmpty(d.onComplete) && state != null)
            state.SetFlag(d.onComplete);
    }

    private DialogueDisplay FindActiveDisplay()
    {
        return FindObjectOfType<DialogueDisplay>();
    }
}
