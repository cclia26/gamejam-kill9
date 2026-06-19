using UnityEngine;

/// <summary>
/// Desktop 场景管理器 — 控制桌面图标显隐、窗口管理。
/// 挂载在 Desktop 场景的 Canvas 上。
/// </summary>
public class DesktopManager : MonoBehaviour
{
    public static DesktopManager Instance { get; private set; }

    private bool _terminalFirstOpen = true;

    [Header("Icons")]
    [SerializeField] private GameObject iconKill9Exe;
    [SerializeField] private GameObject iconSystemLog;
    [SerializeField] private GameObject iconReadme;
    [SerializeField] private GameObject iconBoardFile;

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
        RefreshIconVisibility();
    }

    /// <summary>
    /// 根据游戏流程状态刷新桌面图标显隐。
    /// </summary>
    public void RefreshIconVisibility()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // 系统日志：第三关后可点击
        if (iconSystemLog != null)
            iconSystemLog.SetActive(state.CurrentPhase >= 4);

        // 董事会文件：第三关后出现
        if (iconBoardFile != null)
            iconBoardFile.SetActive(state.boardFileRevealed);
    }

    /// <summary>
    /// 打开终端窗口。
    /// </summary>
    public void OpenTerminal()
    {
        if (_terminalWindow == null && terminalWindowPrefab != null)
        {
            var go = Instantiate(terminalWindowPrefab, transform);
            _terminalWindow = go.GetComponent<TerminalWindow>();
        }

        if (_terminalWindow != null)
            _terminalWindow.Open();

        // 首次打开终端 → 触发开场对话
        if (_terminalFirstOpen)
        {
            _terminalFirstOpen = false;
            var dm = FindObjectOfType<DialogueManager>();
            if (dm != null)
                dm.TriggerDialogues("Desktop");
        }
    }

    /// <summary>
    /// 打开文本查看窗口。
    /// </summary>
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
}
