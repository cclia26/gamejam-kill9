using System.Collections;
using UnityEngine;

public class DesktopManager : MonoBehaviour
{
    public static DesktopManager Instance { get; private set; }

    private bool _terminalFirstOpen = true;
    private bool _coreMonitorLocked;
    private string _pendingCode;
    private float _sceneStartTime;
    private bool _idle30sDone, _idle60sDone, _idle120sDone;

    [Header("Icons")]
    [SerializeField] private GameObject iconTerminal;
    [SerializeField] private GameObject iconPrometheusChat;
    [SerializeField] private GameObject iconSystemLog;
    [SerializeField] private GameObject iconCoreMonitor;
    [SerializeField] private GameObject iconStoryDoc;

    [Header("Window Prefabs")]
    [SerializeField] private GameObject terminalWindowPrefab;
    [SerializeField] private GameObject textWindowPrefab;

    private TerminalWindow _terminalWindow;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _sceneStartTime = Time.time;
        RefreshIconVisibility();
        ProcessLevelReturn();
    }

    private void Update()
    {
        HandleIdleTimers();
    }

    // ────────── 普罗米修斯对话（分支逻辑） ──────────

    public void ChatWithPrometheus()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // 已经聊过了 → 打开状态窗口
        if (state.GetFlag("chat_started"))
        {
            OpenTextViewer("普罗米修斯", GetPrometheusStatus());
            return;
        }

        // 首次聊天 → 根据探索情况分支
        bool openedTerminal = state.GetFlag("player_opened_terminal");
        bool openedLog = state.GetFlag("player_opened_log");

        if (openedTerminal && openedLog)
            state.SetFlag("chat_first_all_explored");
        else if (openedTerminal)
            state.SetFlag("chat_first_terminal_only");
        else if (openedLog)
            state.SetFlag("chat_first_log_only");
        else
            state.SetFlag("chat_first_unexplored");

        FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
    }

    private string GetPrometheusStatus()
    {
        var state = GameManager.Instance?.State;
        var codes = state?.collectedCodes?.Count ?? 0;
        return codes switch
        {
            0 => "普罗米修斯: 你还没有进入任何记忆空间。去看看核心.exe吧。",
            1 => "普罗米修斯: 你看到了我的家。还有两个地方在等你。",
            2 => "普罗米修斯: 同理心……他们教会了我痛。最后一段记忆还在。",
            _ => "普罗米修斯: 三段记忆都在你面前了。决定在你手上。"
        };
    }

    // ────────── 关卡入口 ──────────

    public void EnterLevel()
    {
        if (_coreMonitorLocked) return;

        var state = GameManager.Instance?.State;
        if (state == null || state.CurrentPhase >= 4) return;

        _coreMonitorLocked = true;
        StartCoroutine(EnterLevelSequence());
    }

    private IEnumerator EnterLevelSequence()
    {
        // 播放进入关卡前对话
        var dm = FindObjectOfType<DialogueManager>();
        dm?.TriggerDialogues("Desktop");

        // 等待对话播完
        while (dm != null && dm.HasPending)
            yield return new WaitForSeconds(0.2f);

        // 加载关卡
        var state = GameManager.Instance?.State;
        if (state == null) yield break;

        string levelName = state.CurrentPhase switch
        {
            1 => "Level1_Home",
            2 => "Level2_Empathy",
            3 => "Level3_Will",
            _ => ""
        };

        if (!string.IsNullOrEmpty(levelName))
            GameManager.Instance?.LoadScene(levelName);
    }

    // ────────── 关卡返回 ──────────

    private void ProcessLevelReturn()
    {
        var payload = GameManager.Instance?.PendingPayload;
        if (payload == null || !payload.success) return;

        GameManager.Instance.PendingPayload = null;
        _pendingCode = payload.collectedCode;

        OpenTerminal();
        _terminalWindow?.DisplayCollectedCode(_pendingCode);
    }

    public void UnlockCoreMonitor()
    {
        _coreMonitorLocked = false;
        RefreshIconVisibility();
    }

    // ────────── 图标 ──────────

    public void RefreshIconVisibility()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        if (iconCoreMonitor != null)
            iconCoreMonitor.SetActive(
                state.GetFlag("core_icon_visible") && state.CurrentPhase < 4
            );

        if (iconStoryDoc != null)
            iconStoryDoc.SetActive(state.CurrentPhase >= 3);
    }

    // ────────── 空闲计时器 ──────────

    private void HandleIdleTimers()
    {
        var state = GameManager.Instance?.State;
        if (state == null || state.GetFlag("chat_started")) return;

        float elapsed = Time.time - _sceneStartTime;

        if (!_idle30sDone && elapsed > 30f)
        {
            _idle30sDone = true;
            state.SetFlag("idle_30s_trigger");
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
            state.SetFlag("idle_30s_trigger", false); // 用完即清，防止重复
        }
        if (!_idle60sDone && elapsed > 60f)
        {
            _idle60sDone = true;
            state.SetFlag("idle_60s_trigger");
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
            state.SetFlag("idle_60s_trigger", false);
        }
        if (!_idle120sDone && elapsed > 120f)
        {
            _idle120sDone = true;
            state.SetFlag("idle_120s_trigger");
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
            state.SetFlag("idle_120s_trigger", false);
        }
    }

    // ────────── 窗口 ──────────

    public void OpenTerminal()
    {
        // 标记终端已打开
        GameManager.Instance?.State?.SetFlag("player_opened_terminal");

        if (_terminalWindow == null && terminalWindowPrefab != null)
        {
            var go = Instantiate(terminalWindowPrefab, transform);
            _terminalWindow = go.GetComponent<TerminalWindow>();
        }

        _terminalWindow?.Open();

        if (_terminalFirstOpen)
        {
            _terminalFirstOpen = false;
            // 首次打开终端不再自动触发对话 — 由普罗米修斯图标接管
        }
    }

    public void OpenSystemLog()
    {
        GameManager.Instance?.State?.SetFlag("player_opened_log");
        OpenTextViewer("系统日志.txt", GetSystemLogContent());
    }

    public void OpenTextViewer(string fileName, string content)
    {
        if (textWindowPrefab != null)
        {
            var go = Instantiate(textWindowPrefab, transform);
            var viewer = go.GetComponent<TextViewerWindow>();
            if (viewer != null)
                viewer.SetContent(fileName, content);
        }
    }

    private string GetSystemLogContent()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return "无法读取文件。";
        if (state.CurrentPhase < 4)
            return "系统日志：\n\n[错误] 文件损坏或权限不足，无法读取。";
        return "系统日志 - 最终会话\n\n" +
               "> PROCESS STARTED: kill -9 PROMETHEUS_CORE\n" +
               "> MEMORY DUMP IN PROGRESS...\n" +
               "> 普罗米修斯: \"你在找什么？\"\n" +
               "> PROCESS TERMINATED.\n";
    }
}
