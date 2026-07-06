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
            .Where(d => !ShouldSuppressInitialDesktopDialogue(d, state))
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


    private bool ShouldSuppressInitialDesktopDialogue(DialogueLine line, GameState state)
    {
        if (line == null || state == null) return false;
        if (line.triggerScene != "Desktop") return false;
        if (!state.GetFlag("desktop_initial_dialogue_locked") && state.CurrentPhase <= 1) return false;

        return IsInitialDesktopDialogue(line);
    }

    private bool IsInitialDesktopDialogue(DialogueLine line)
    {
        string id = line.id ?? string.Empty;
        string condition = line.triggerCondition ?? string.Empty;

        return id.StartsWith("chat_")
            || id.StartsWith("idle_30s")
            || id.StartsWith("idle_60s")
            || id.StartsWith("idle_120s")
            || id.StartsWith("sorry_before")
            || condition.StartsWith("chat_")
            || condition.StartsWith("c1_")
            || condition.StartsWith("cu1_")
            || condition.StartsWith("ct1_")
            || condition.StartsWith("cl1_")
            || condition.StartsWith("ce1_")
            || condition.StartsWith("i1_")
            || condition.StartsWith("i2_")
            || condition.StartsWith("i3_")
            || condition.StartsWith("i4_")
            || condition.StartsWith("idle30")
            || condition.StartsWith("idle60")
            || condition.StartsWith("idle120")
            || condition.StartsWith("sorry_")
            || condition == "player_said_sorry_early";
    }
    private IEnumerator ProcessQueue()
    {
        _isShowing = true;
        var display = FindActiveDisplay();

        while (_pendingQueue.Count > 0)
        {
            var d = _pendingQueue.Dequeue();
            yield return ShowDialogue(d, display);

            // 每条对话播完后重新扫描，捕捉被 onComplete flag 解锁的新对话
            var state = GameManager.Instance?.State;
            if (state != null)
            {
                var more = _allDialogues
                    .Where(line => line.playOnce) // 重扫只匹配一次性对话，playOnce=false 的靠外部触发
                    .Where(line => !_playedIds.Contains(line.id))
                    .Where(line => line.triggerScene == d.triggerScene || string.IsNullOrEmpty(line.triggerScene))
                    .Where(line => !ShouldSuppressInitialDesktopDialogue(line, state))
                    .Where(line => string.IsNullOrEmpty(line.triggerCondition) || state.GetFlag(line.triggerCondition))
                    .Where(line => line.triggerEnterCount < 0 || state.enterCount >= line.triggerEnterCount)
                    .OrderByDescending(line => line.priority);

                foreach (var line in more)
                {
                    _pendingQueue.Enqueue(line);
                    if (line.playOnce)
                        _playedIds.Add(line.id);
                }
            }
        }

        _isShowing = false;
    }

    private IEnumerator ShowDialogue(DialogueLine d, DialogueDisplay display)
    {
        var state = GameManager.Instance?.State;

        // 直接输出文本，不添加说话者前缀
        string displayText = d.text;

        float speed = 0.04f * d.delay;

        // 运行时替换：系统时间占位符 → 真实时间
        displayText = ReplaceTimePlaceholders(displayText);

        // 空文本跳过显示（如 third_enter_fake），只等一帧完成回调
        if (!string.IsNullOrEmpty(displayText))
        {
            if (display != null)
            {
                yield return display.PlayText(displayText, speed);
            }
            else
            {
                Debug.Log($"[Dialogue] {displayText}");
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return null;
        }

        // 完成回调：设置 flag + 刷新桌面图标
        if (!string.IsNullOrEmpty(d.onComplete) && state != null)
        {
            state.SetFlag(d.onComplete);
            DesktopManager.Instance?.RefreshIconVisibility();
        }
    }

    private string ReplaceTimePlaceholders(string text)
    {
        // "20xx年xx月xx日，晚上xx:xx" → 真实系统时间
        var now = System.DateTime.Now;
        string dateStr = now.ToString("yyyy年MM月dd日，HH:mm");
        text = text.Replace("20xx年xx月xx日，晚上xx:xx", dateStr);

        return text;
    }

    private DialogueDisplay FindActiveDisplay()
    {
        var displays = FindObjectsOfType<DialogueDisplay>();
        if (displays == null || displays.Length == 0)
        {
            return null;
        }

        return displays
            .OrderByDescending(d => d.GetComponentInParent<Canvas>() != null ? d.GetComponentInParent<Canvas>().sortingOrder : 0)
            .ThenByDescending(d => d.transform.GetSiblingIndex())
            .FirstOrDefault();
    }
}
