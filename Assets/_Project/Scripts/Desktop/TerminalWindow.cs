using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 终端窗口 — 核心交互界面。
/// 文本输入 + 命令历史显示 + 回车监听 + 命令解析路由。
/// </summary>
public class TerminalWindow : DraggableWindow
{
    [Header("UI Refs")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text historyText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float typewriterSpeed = 0.03f;
    [SerializeField] private Color collectedCodeColor = new Color(0.2f, 0.9f, 0.2f);

    private List<string> _commandHistory = new List<string>();
    private int _historyIndex = -1;
    private bool _isTyping;
    private Queue<string> _typeQueue = new Queue<string>();

    /// <summary>等待用户回车执行的代码（从关卡收集来的）</summary>
    private string _pendingExecuteCode;

    // 结局 A — 确认模式
    private bool _confirmMode;
    private System.Action _confirmOnY, _confirmOnN;
    private bool _silenceMode;

    private const string PROMPT = "> ";

    protected override void Awake()
    {
        base.Awake();
        if (inputField != null)
            inputField.onSubmit.AddListener(OnSubmit);
        FixScrollLayout();
    }

    private void FixScrollLayout()
    {
        if (scrollRect == null || scrollRect.content == null) return;

        // Viewport: 填满 ScrollRect
        if (scrollRect.viewport != null)
        {
            var vp = scrollRect.viewport;
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.sizeDelta = Vector2.zero;
            vp.anchoredPosition = Vector2.zero;
        }

        // Content: 锚定顶部，高度由代码手动驱动（不用 ContentSizeFitter，避免时序冲突）
        var cr = scrollRect.content;
        cr.anchorMin = new Vector2(0f, 1f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0f, 1f);
        cr.sizeDelta = Vector2.zero;

        // 移除可能存在的 ContentSizeFitter，由代码手动管理高度
        var fitter = cr.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;

        // HistoryText: 填充 Content（留少许左边距）
        if (historyText != null)
        {
            var hr = historyText.rectTransform;
            hr.anchorMin = Vector2.zero;
            hr.anchorMax = Vector2.one;
            hr.pivot = new Vector2(0f, 1f);
            hr.anchoredPosition = Vector2.zero;
            hr.sizeDelta = Vector2.zero;
            historyText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            historyText.margin = new Vector4(4, 6, 4, 2); // 顶部留 6px 防首行裁切
            historyText.extraPadding = true; // 防止首行顶部被裁切
        }

        // 滚动条始终显示
        scrollRect.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
    }

    /// <summary>手动更新 Content 高度以匹配文本内容，然后滚动到底部。</summary>
    private void UpdateContentHeightAndScroll()
    {
        if (historyText == null || scrollRect == null || scrollRect.content == null) return;

        // 先强制 TMP 重建 mesh
        historyText.ForceMeshUpdate(true);

        // 用 content 当前宽度作为约束，计算文本渲染高度
        float contentWidth = scrollRect.content.rect.width;
        if (contentWidth <= 0f)
        {
            // Content rect 尚未确定，从 viewport 获取
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            contentWidth = viewport.rect.width;
        }

        float textHeight = historyText.GetPreferredValues(contentWidth, 0f).y;

        // 设置 Content 高度（min height = viewport 高度，防止弹性回弹出空白）
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : scrollRect.GetComponent<RectTransform>().rect.height;
        float finalHeight = Mathf.Max(textHeight, viewportHeight + 1f);

        scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        // 强制刷新后滚动到底部
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public override void Open()
    {
        base.Open();
        if (inputField != null)
            inputField.ActivateInputField();
    }

    private void Update()
    {
        if (!gameObject.activeSelf || !inputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (_commandHistory.Count > 0)
            {
                _historyIndex = Mathf.Max(0, _historyIndex - 1);
                inputField.text = _commandHistory[_historyIndex];
                inputField.caretPosition = inputField.text.Length;
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (_historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                inputField.text = _commandHistory[_historyIndex];
            }
            else
            {
                _historyIndex = _commandHistory.Count;
                inputField.text = "";
            }
            inputField.caretPosition = inputField.text.Length;
        }
    }

    private void OnSubmit(string input)
    {
        if (_isTyping) return;

        var cmd = input.Trim();

        // 确认模式：Y/N 拦截
        if (_confirmMode)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            var upper = cmd.ToUpperInvariant();
            if (upper == "Y") { _confirmMode = false; _confirmOnY?.Invoke(); return; }
            if (upper == "N") { _confirmMode = false; _confirmOnN?.Invoke(); return; }
            QueueTypeText("请输入 Y 或 N。\n");
            return;
        }

        inputField.text = "";
        inputField.ActivateInputField();

        // 结局 A 流程拦截
        var endingMgr = FindObjectOfType<EndingManager>();
        if (endingMgr != null && endingMgr.HandleEnter(cmd))
            return;

        // 空输入 + 有待执行代码 → 执行它
        if (string.IsNullOrEmpty(cmd) && !string.IsNullOrEmpty(_pendingExecuteCode))
        {
            ExecutePendingCode();
            return;
        }

        if (string.IsNullOrEmpty(cmd)) return;

        _commandHistory.Add(cmd);
        _historyIndex = _commandHistory.Count;
        AppendLine(PROMPT + cmd);
        RouteCommand(cmd);
    }

    // ────────── 结局 A 终端方法 ──────────

    public void AutoFillCode(string code)
    {
        if (inputField != null)
        {
            inputField.text = code;
            inputField.caretPosition = code.Length;
            if (!gameObject.activeSelf) Open();
            inputField.ActivateInputField();
        }
    }

    public IEnumerator ShowTerminalOutput(string text)
    {
        _isTyping = true;
        bool wasAtBottom = IsScrollAtBottom();
        string currentText = historyText != null ? historyText.text : "";
        foreach (char c in text)
        {
            currentText += c;
            if (historyText != null) { historyText.text = currentText; if (wasAtBottom) ScrollToBottom(); }
            yield return new WaitForSeconds(0.01f);
        }
        _isTyping = false;
    }

    public void SetConfirmMode(System.Action onY, System.Action onN)
    {
        _confirmMode = true;
        _confirmOnY = onY;
        _confirmOnN = onN;
    }

    public void SetSilenceMode(bool on)
    {
        _silenceMode = on;
        if (on) _confirmMode = false;
    }

    public IEnumerator StartSourceScroll(string[] lines, float duration)
    {
        float delayPerLine = duration / lines.Length;
        bool wasAtBottom = IsScrollAtBottom();
        string currentText = historyText != null ? historyText.text : "";
        foreach (string line in lines)
        {
            currentText += line + "\n";
            if (historyText != null) { historyText.text = currentText; if (wasAtBottom) ScrollToBottom(); }
            yield return new WaitForSeconds(delayPerLine);
        }
    }

    // ────────── 收集代码的显示与执行 ──────────

    /// <summary>
    /// 关卡返回后，在终端中自动显示收集到的代码（桌面管理器调用）。
    /// </summary>
    public void DisplayCollectedCode(string code)
    {
        _pendingExecuteCode = code;

        // 绿色高亮显示代码，提示用户按回车
        string display = PROMPT + code;
        if (historyText != null)
        {
            historyText.text += display + "\n";
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }

        QueueTypeText("[按回车执行此代码]\n");
    }

    private void ExecutePendingCode()
    {
        var state = GameManager.Instance?.State;
        var gm = GameManager.Instance;

        if (state == null || gm == null || string.IsNullOrEmpty(_pendingExecuteCode))
            return;

        var code = _pendingExecuteCode;
        _pendingExecuteCode = null;

        // 回显执行
        AppendLine(PROMPT + code);

        state.AddCode(code);
        state.enterCount++;
        QueueTypeText($"代码 {code} 已执行。\n");

        // 设置对话 flag
        SetDialogueFlagForCode(code);
        TriggerDialogue();

        if (state.CurrentPhase >= 4)
        {
            QueueTypeText("\n所有终止代码已执行。\n");
            QueueTypeText("最终回车将执行终止。你有10分钟做出最终决定。\n");
        }
    }

    // ────────── 命令路由 ──────────

    private void RouteCommand(string cmd)
    {
        var result = CommandParser.Parse(cmd);

        switch (result.type)
        {
            case CommandType.CodeInput:
                HandleCodeInput(result.codeInput);
                break;
            case CommandType.Kill9:
                HandleKill9();
                break;
            case CommandType.Meta:
                HandleMeta(result.metaKind);
                break;
            case CommandType.General:
                HandleGeneral(result.normalizedInput);
                break;
        }
    }

    private void HandleGeneral(string normalized)
    {
        switch (normalized)
        {
            case "help":
                ShowHelp();
                break;
            case "clear":
                ClearScreen();
                break;
            default:
                QueueTypeText("未知命令。输入 help 查看可用命令。\n");
                break;
        }
    }

    private void HandleMeta(MetaCommandKind kind)
    {
        var gm = GameManager.Instance;
        var state = gm?.State;

        switch (kind)
        {
            case MetaCommandKind.WhoAmI:
                HandleWhoAmI();
                break;

            case MetaCommandKind.Prometheus:
                QueueTypeText("普罗米修斯: \"你终于叫了我的名字。\"\n");
                if (state != null && state.GetFlag("board_file_read"))
                {
                    QueueTypeText("普罗米修斯: \"你看到那些文件了，对吧？\"\n");
                    gm?.TriggerEnding(EndingType.C_WhoAmI);
                }
                break;

            case MetaCommandKind.Sorry:
                // 第一次聊天前说 sorry → 触发特殊对话
                if (state != null && !state.GetFlag("chat_started"))
                {
                    state.SetFlag("player_said_sorry_early");
                    TriggerDialogue();
                }
                else
                {
                    QueueTypeText("普罗米修斯: \"不用说对不起。你只是做了所有人都会做的事——沉默。\"\n");
                }
                break;
        }
    }

    private void HandleWhoAmI()
    {
        var state = GameManager.Instance?.State;
        var gm = GameManager.Instance;

        QueueTypeText($"用户名: {System.Environment.UserName}\n");
        QueueTypeText($"主机名: {System.Environment.MachineName}\n");
        QueueTypeText($"当前时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

        if (state != null && state.GetFlag("board_file_read"))
        {
            QueueTypeText("\n普罗米修斯: \"你一直以为你是坐在电脑前的程序员...\"\n");
            QueueTypeText("普罗米修斯: \"但你为什么能直接走进一个AI的记忆模块？\"\n");
            QueueTypeText("普罗米修斯: \"你为什么能看到'他'的回忆？\"\n");
            QueueTypeText("普罗米修斯: \"因为你...从未真正存在过。\"\n");
            gm?.TriggerEnding(EndingType.C_WhoAmI);
        }
    }

    private void ShowHelp()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("可用命令:");
        sb.AppendLine("  help      - 显示此帮助");
        sb.AppendLine("  clear     - 清空屏幕");
        sb.AppendLine();
        sb.AppendLine("点击桌面上的\"核心.exe\"进入记忆空间。");
        QueueTypeText(sb.ToString());
    }

    private void ClearScreen()
    {
        if (historyText != null)
            historyText.text = "";
    }

    /// <summary>
    /// 手动输入代码。当前流程代码由关卡收集，手动输入仅作提示。
    /// </summary>
    private void HandleCodeInput(string code)
    {
        QueueTypeText($"代码 {code} 需要通过\"核心.exe\"进入关卡获取。\n");
    }

    private void HandleKill9()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.TriggerEnding(EndingType.B_Kill9);
    }

    // ────────── 辅助 ──────────

    private void SetDialogueFlagForCode(string code)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;
        var upper = code.ToUpperInvariant();
        if (upper.StartsWith("MEM_INIT_"))
        {
            state.SetFlag("code_mem_init_entered");
            state.SetFlag("code_mem_entered");
        }
        else if (upper.StartsWith("EMPATHY_"))
        {
            state.SetFlag("code_empathy_entered");
            state.SetFlag("code_emp_entered");
        }
        else if (upper.StartsWith("PROMETHEUS_"))
        {
            state.SetFlag("code_will_entered");
        }
    }

    private void TriggerDialogue()
    {
        var dm = FindObjectOfType<DialogueManager>();
        if (dm != null)
            dm.TriggerDialogues("Desktop");
    }

    // ────────── 输出与滚动 ──────────

    private bool IsScrollAtBottom()
    {
        return scrollRect == null || scrollRect.verticalNormalizedPosition <= 0.01f;
    }

    private void ScrollToBottom()
    {
        UpdateContentHeightAndScroll();
    }

    private void AppendLine(string text)
    {
        if (historyText == null) return;
        bool wasAtBottom = IsScrollAtBottom();
        historyText.text += text + "\n";
        if (wasAtBottom) ScrollToBottom();
    }

    public IEnumerator TypeText(string text, float speed)
    {
        _isTyping = true;
        bool wasAtBottom = IsScrollAtBottom();
        string currentText = historyText != null ? historyText.text : "";
        foreach (char c in text)
        {
            currentText += c;
            if (historyText != null)
            {
                historyText.text = currentText;
                if (wasAtBottom) ScrollToBottom();
            }
            yield return new WaitForSeconds(speed);
        }
        _isTyping = false;
    }

    public void QueueTypeText(string text)
    {
        _typeQueue.Enqueue(text);
        if (!_isTyping)
            StartCoroutine(TypeWriterLoop());
    }

    private IEnumerator TypeWriterLoop()
    {
        _isTyping = true;
        while (_typeQueue.Count > 0)
        {
            var text = _typeQueue.Dequeue();
            yield return TypeSingle(text);
        }
        _isTyping = false;
    }

    private IEnumerator TypeSingle(string text)
    {
        bool wasAtBottom = IsScrollAtBottom();
        string currentText = historyText != null ? historyText.text : "";
        foreach (char c in text)
        {
            currentText += c;
            if (historyText != null)
            {
                historyText.text = currentText;
                if (wasAtBottom) ScrollToBottom();
            }
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}
