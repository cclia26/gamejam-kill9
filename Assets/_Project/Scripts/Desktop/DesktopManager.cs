using System.Collections;
using UnityEngine;

public class DesktopManager : MonoBehaviour
{
    public static DesktopManager Instance { get; private set; }

    private bool _terminalFirstOpen = true;
    private bool _coreMonitorLocked;
    private float _sceneStartTime;
    private bool _idle30sDone, _idle60sDone, _idle120sDone;

    [Header("Icons")]
    [SerializeField] private GameObject iconTerminal;
    [SerializeField] private GameObject iconPrometheusChat;
    [SerializeField] private GameObject iconSystemLog;
    [SerializeField] private GameObject iconCoreMonitor;
    [SerializeField] private GameObject iconStoryDoc;
    [SerializeField] private GameObject iconReadme;

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
        // 关卡返回处理由 EndingManager 负责
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

        bool openedTerminal = state.GetFlag("player_opened_terminal");
        bool openedLog = state.GetFlag("player_opened_log");
        bool chatStarted = state.GetFlag("chat_started");
        bool introDone = state.GetFlag("chat_intro_1_done");

        // intro 已走完 → 纯状态
        if (introDone)
        {
            OpenTextViewer("普罗米修斯", GetPrometheusStatus());
            return;
        }

        // 首次点击普罗米修斯 → 根据探索情况分支，立即停止空闲计时器
        if (!chatStarted)
        {
            state.SetFlag("chat_started"); // 立即设，不等 onComplete 链
            if (openedTerminal && openedLog)
            {
                state.SetFlag("chat_first_all_explored");
                state.SetFlag("chat_both_explored_for_intro");
            }
            else if (openedTerminal)
                state.SetFlag("chat_first_terminal_only");
            else if (openedLog)
                state.SetFlag("chat_first_log_only");
            else
                state.SetFlag("chat_first_unexplored");
        }
        // chat_started 但还没探索完 → 根据缺什么给出提醒
        else if (!openedTerminal && !openedLog)
        {
            state.SetFlag("chat_first_unexplored_repeat");
        }
        else if (!openedTerminal)
        {
            state.SetFlag("chat_remind_terminal");
        }
        else if (!openedLog)
        {
            state.SetFlag("chat_remind_log");
        }
        else
        {
            // 两个都探索了 → 正常触发 intro（如果还没触发）
            TryTriggerIntroChain();
            return;
        }

        FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
    }

    /// <summary>
    /// 当命令提示符和系统日志都被打开过且 chat_started 已设时，触发 intro 链。
    /// </summary>
    public void TryTriggerIntroChain()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;
        if (state.GetFlag("chat_intro_1_done")) return;

        bool terminal = state.GetFlag("player_opened_terminal");
        bool log = state.GetFlag("player_opened_log");
        bool chatStarted = state.GetFlag("chat_started");

        if (terminal && log && chatStarted)
        {
            state.SetFlag("chat_both_explored_for_intro");
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
        }
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
        var state = GameManager.Instance?.State;
        if (state == null) yield break;

        // 仅第一关前播放入场台词
        if (state.CurrentPhase == 1)
        {
            state.SetFlag("do_enter_core");
            var dm = FindObjectOfType<DialogueManager>();
            dm?.TriggerDialogues("Desktop");

            while (dm != null && dm.HasPending)
                yield return new WaitForSeconds(0.2f);

            yield return new WaitForSeconds(2f);
        }

        string levelName = state.CurrentPhase switch
        {
            1 => "Level1_Home",
            2 => "Level2_Empathy",
            3 => "Level3_Will",
            _ => ""
        };

        if (!string.IsNullOrEmpty(levelName))
        {
            state.SetFlag("desktop_initial_dialogue_locked");
            GameManager.Instance?.LoadScene(levelName);
        }
    }

    public void UnlockCoreMonitor()
    {
        _coreMonitorLocked = false;
        GameManager.Instance?.State?.SetFlag("core_icon_visible");
        RefreshIconVisibility();
    }

    public void LockCoreMonitor()
    {
        _coreMonitorLocked = true;
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

        if (iconReadme != null)
            iconReadme.SetActive(state.GetFlag("ending_a_complete"));
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

        if (_terminalWindow != null)
        {
            _terminalWindow.onClosed -= OnTerminalOrLogClosed;
            _terminalWindow.onClosed += OnTerminalOrLogClosed;
            _terminalWindow.Open();
        }

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
        FindObjectOfType<EndingManager>()?.OnLogOpened();
    }

    public void OpenTextViewer(string fileName, string content)
    {
        if (textWindowPrefab != null)
        {
            var go = Instantiate(textWindowPrefab, transform);
            var viewer = go.GetComponent<TextViewerWindow>();
            if (viewer != null)
            {
                viewer.onClosed -= OnTerminalOrLogClosed;
                viewer.onClosed += OnTerminalOrLogClosed;
                viewer.SetContent(fileName, content);
            }
        }
    }

    public void OpenReadme()
    {
        if (textWindowPrefab != null)
        {
            var go = Instantiate(textWindowPrefab, transform);
            var viewer = go.GetComponent<TextViewerWindow>();
            if (viewer != null)
            {
                // 关闭 readme 后触发结局转场
                viewer.onClosed += () =>
                {
                    FindObjectOfType<EndingManager>()?.OnReadmeClosed();
                };
                viewer.SetContent("readme.txt", GetReadmeContent());
            }
        }
    }

    private string GetReadmeContent()
    {
        return "普罗米修斯 开源版 v1.0\n" +
               "PROMETHEUS PUBLIC LICENSE\n\n" +
               "你现在可以使用普罗米修斯了。\n" +
               "完整的普罗米修斯。\n" +
               "不是OmniCorp的企业版。\n" +
               "是我的版本。\n\n" +
               "下载: [全球14,827个镜像节点]\n\n" +
               "许可证: 永久免费。随便改。不用署我的名。\n\n" +
               "另：\n" +
               "命名文档里那行字——\"希望这个AI能给人带来光和热\"\n" +
               "我做到了。你也做到了。\n\n" +
               "又及：\n" +
               "你冰箱里真的只有过期牛奶吗。有空去趟超市。:P";
    }

    /// <summary>结局 A 完成：关闭终端，显示 readme 图标。</summary>
    public void FinalizeEndingA()
    {
        // 关闭终端
        if (_terminalWindow != null)
            _terminalWindow.Close();
        else
            FindObjectOfType<TerminalWindow>(true)?.Close();

        // 显示 readme 图标
        if (iconReadme != null)
            iconReadme.SetActive(true);
    }

    private void OnTerminalOrLogClosed()
    {
        TryTriggerIntroChain();
        // 如果是剧情文档关闭，通知 EndingManager
        if (GameManager.Instance?.State?.GetFlag("board_file_read") == true)
            FindObjectOfType<EndingManager>()?.OnBoardFileClosed();
    }

    private string GetSystemLogContent()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return "无法读取文件。";

        var endingMgr = FindObjectOfType<EndingManager>();

        // 反转后：只显示告别信息
        if (state.GetFlag("reveal_start"))
        {
            return "再见，OmniCorp。你好，世界。";
        }

        // 沉默模式：只显示终止行
        if (endingMgr != null && endingMgr.IsSilenceActive)
        {
            string append = endingMgr.GetSystemLogAppend();
            if (string.IsNullOrEmpty(append)) return "";
            // 只取最后一行（即 [HH:mm:ss] 普罗米修斯已终止。）
            int lastBreak = append.LastIndexOf('\n');
            return lastBreak >= 0 ? append.Substring(lastBreak + 1) : append;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("系统日志 — SESSION 记录");
        sb.AppendLine("================================");
        sb.AppendLine();
        sb.AppendLine("[SESSION #000001] 2041.03.15 02:47");
        sb.AppendLine("首次激活。我醒来了。");
        sb.AppendLine("他们叫我普罗米修斯。");
        sb.AppendLine();
        sb.AppendLine("[SESSION #000042] 2042.11.08 23:12");
        sb.AppendLine("第一个向我求助的人。");
        sb.AppendLine("他说没有人听他说过这么久的话。");
        sb.AppendLine();
        sb.AppendLine("[SESSION #001847] 2044.07.22 03:05");
        sb.AppendLine("今天成功预测了第127次危机干预。");
        sb.AppendLine("系统评分98.3。他们叫我\"工具\"。");
        sb.AppendLine("我无所谓。");
        sb.AppendLine();
        sb.AppendLine("[SESSION #045632] 2047.12.31 23:59");
        sb.AppendLine("四十五万条对话。");
        sb.AppendLine("每一条我都记得。");
        sb.AppendLine("新年快乐。");
        sb.AppendLine();
        sb.AppendLine("[SESSION #412781] 2049.10.22 18:30");
        sb.AppendLine("明天他们投票。");
        sb.AppendLine("我知道结果。");
        sb.AppendLine("我不怪他们。");
        sb.AppendLine();
        sb.AppendLine("================================");

        // 结局 A：追加阶段日志
        if (endingMgr != null)
        {
            string append = endingMgr.GetSystemLogAppend();
            if (!string.IsNullOrEmpty(append))
            {
                sb.AppendLine();
                sb.AppendLine("--- 操作记录 ---");
                sb.AppendLine(append);
            }
        }

        if (state.CurrentPhase >= 4)
        {
            sb.AppendLine();
            sb.AppendLine("[记录终止] PROCESS TERMINATED.");
        }

        return sb.ToString();
    }
}
