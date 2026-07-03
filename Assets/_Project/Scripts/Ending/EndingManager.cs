using System.Collections;
using UnityEngine;

/// <summary>
/// 结局 A 流程控制器 — 关卡返回后的对话、空闲计时器、终端自动填入、沉默模式、反转演出。
/// 挂载在 GameManager 所在的 GameObject 上。
/// </summary>
public class EndingManager : MonoBehaviour
{
    [Header("Timers")]
    [SerializeField] private float idle10s = 10f;
    [SerializeField] private float idle20s = 20f;
    [SerializeField] private float idle40s = 40f;
    [SerializeField] private float silenceDuration = 10f;

    private int _currentPhase; // 1, 2, 3
    private bool _phaseActive;
    private bool _codeEntered;
    private float _phaseStartTime;
    private bool _idle10Done, _idle20Done, _idle40Done;
    private int _total40sCount; // 40s 累计触发次数（跨关）
    private int _logOpenCount;
    private bool _silenceActive;
    private bool _revealRunning;

    private const string PROMPT_PATH = "C:\\OMNICORP\\PROMETHEUS> ";

    private void Start()
    {
        // 检查是否有返回的关卡数据
        ProcessLevelReturn();
    }

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private void Update()
    {
        // ── 临时测试快捷键 ──
        if (enableDebugKeys)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                FakeLevelReturn(1, "MEM_INIT_20491023");
            if (Input.GetKeyDown(KeyCode.Alpha2))
                FakeLevelReturn(2, "EMPATHY_CORE_V3");
            if (Input.GetKeyDown(KeyCode.Alpha3))
                FakeLevelReturn(3, "PROMETHEUS_CORE_WILL");
        }

        // 检测唤醒 flag，自动唤起终端填入代码
        CheckWakeFlags();

        if (!_phaseActive || _codeEntered || _silenceActive) return;
        float elapsed = Time.time - _phaseStartTime;
        var state = GameManager.Instance?.State;
        if (state == null) return;

        if (!_idle10Done && elapsed > idle10s)
        {
            _idle10Done = true;
            TriggerIdle(10);
        }
        if (!_idle20Done && elapsed > idle20s)
        {
            _idle20Done = true;
            TriggerIdle(20);
        }
        if (!_idle40Done && elapsed > idle40s)
        {
            _idle40Done = true;
            _total40sCount++;
            TriggerIdle(40);
            // 唤醒终端由 idle_*_40s_a 的 onComplete flag 触发 CheckWakeFlags 处理
        }
    }

    // ── 临时测试 ──
    private void FakeLevelReturn(int phase, string code)
    {
        Debug.Log($"[EndingA Test] 模拟关卡 {phase} 返回，代码: {code}");
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // 确保 CurrentPhase 匹配
        while (state.CurrentPhase < phase)
            state.collectedCodes.Add("test_placeholder");
        while (state.CurrentPhase > phase && state.collectedCodes.Count > 0)
            state.collectedCodes.RemoveAt(state.collectedCodes.Count - 1);

        // 重置流程状态
        _phaseActive = false;
        _codeEntered = false;
        _silenceActive = false;
        _revealRunning = false;

        // 模拟关卡返回
        GameManager.Instance.PendingPayload = new ScenePayload { success = true, collectedCode = code };
        ProcessLevelReturn();
    }

    // ────────── 关卡返回处理 ──────────

    public void ProcessLevelReturn()
    {
        var payload = GameManager.Instance?.PendingPayload;
        if (payload == null || !payload.success) return;

        GameManager.Instance.PendingPayload = null;
        var state = GameManager.Instance?.State;
        if (state == null) return;

        string code = payload.collectedCode;
        _currentPhase = state.CurrentPhase;

        // 重置状态
        _codeEntered = false;
        _silenceActive = false;
        _revealRunning = false;
        _idle10Done = _idle20Done = _idle40Done = false;
_logOpenCount = 0;

        // 根据阶段设置 flag
        switch (_currentPhase)
        {
            case 1: state.SetFlag("code_mem_collected"); break;
            case 2: state.SetFlag("code_emp_collected"); break;
            case 3: state.SetFlag("code_will_collected"); break;
        }

        // 触发返回对话（return_level1 等）
        var dialogueMgr = FindObjectOfType<DialogueManager>();
        dialogueMgr?.TriggerDialogues("Desktop");

        // 对话播完 → 打开终端显示文本并填入代码 → 启动空闲计时
        StartCoroutine(OpenTerminalAndStartTimers(code));
    }

    private IEnumerator StartTimersAfterSetup()
    {
        var dialogueMgr = FindObjectOfType<DialogueManager>();
        // 等对话队列清空才开始计时
        while (dialogueMgr != null && dialogueMgr.HasPending)
            yield return new WaitForSeconds(0.5f);
        // 额外等一小段，给玩家反应时间
        yield return new WaitForSeconds(0.5f);
        _phaseStartTime = Time.time;
        _phaseActive = true;
    }

    private IEnumerator OpenTerminalAndStartTimers(string code)
    {
        // 等待返回对话播完
        var dialogueMgr = FindObjectOfType<DialogueManager>();
        while (dialogueMgr != null && dialogueMgr.HasPending)
            yield return new WaitForSeconds(0.3f);

        // 打开终端，显示阶段文本并填入代码
        DesktopManager.Instance?.OpenTerminal();
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (terminal != null)
        {
            yield return terminal.ShowTerminalOutput(GetPhaseTerminalText());
            terminal.AutoFillCode(code);
            // 确保对话面板在终端上方
            var display = FindObjectOfType<DialogueDisplay>(true);
            if (display != null) display.transform.SetAsLastSibling();
        }

        // 启动空闲计时器
        yield return new WaitForSeconds(0.5f);
        _phaseStartTime = Time.time;
        _phaseActive = true;
    }

    private string GetPhaseTerminalText()
    {
        return _currentPhase switch
        {
            1 => PROMPT_PATH + "等待操作员输入第一段终止代码...\n" + PROMPT_PATH + "_",
            2 => PROMPT_PATH + "终止代码执行进度: 1/3\n" +
                 PROMPT_PATH + "等待操作员输入第二段终止代码...\n" + PROMPT_PATH + "_",
            3 => PROMPT_PATH + "终止代码执行进度: 2/3\n" +
                 PROMPT_PATH + "已收集全部终止代码。\n" +
                 PROMPT_PATH + "请输入最后一段终止代码: PROMETHEUS_CORE_WILL\n" +
                 PROMPT_PATH + "\n注意: 此操作不可撤销。",
            _ => ""
        };
    }

    // ────────── 空闲计时器 ──────────

    private void StartPhaseTimers()
    {
        _phaseActive = true;
        _codeEntered = false;
        _phaseStartTime = Time.time;
        _idle10Done = _idle20Done = _idle40Done = false;
_logOpenCount = 0;
    }

    private void TriggerIdle(int seconds)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        string flag = null;
        if (_currentPhase == 1)
        {
            if (seconds == 10) flag = "idle_lv1_10s_trig";
            else if (seconds == 20) flag = "idle_lv1_20s_trig";
            else if (seconds == 40) flag = "idle_lv1_40s_trig";
        }
        else if (_currentPhase == 2)
        {
            if (seconds == 10) flag = "idle_lv2_10s_trig";
            else if (seconds == 40 && _total40sCount == 1) flag = "idle_lv2_40s_1_trig";
            else if (seconds == 40) flag = "idle_lv2_40s_2_trig";
        }
        else if (_currentPhase == 3)
        {
            if (seconds == 10) flag = "idle_lv3_board_trig";
            else if (seconds == 40 && _total40sCount == 1) flag = "idle_lv3_40s_1_trig";
            else if (seconds == 40 && _total40sCount == 2) flag = "idle_lv3_40s_2_trig";
            else if (seconds == 40) flag = "idle_lv3_40s_3_trig";
        }

        if (!string.IsNullOrEmpty(flag))
        {
            state.SetFlag(flag);
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
        }
    }

    private void CheckWakeFlags()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;
        string[] wakeFlags = { "wake_lv1_40s", "wake_lv2_40s_1", "wake_lv2_40s_2",
                               "wake_lv3_40s_1", "wake_lv3_40s_2", "wake_lv3_40s_3" };
        foreach (var f in wakeFlags)
        {
            if (state.GetFlag(f))
            {
                state.SetFlag(f, false);
                AutoFillCodeInTerminal();
                break;
            }
        }
    }

    private void AutoFillCodeInTerminal()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        string code = _currentPhase switch
        {
            1 => "MEM_INIT_20491023",
            2 => "EMPATHY_CORE_V3",
            3 => "PROMETHEUS_CORE_WILL",
            _ => ""
        };

        // 确保终端存在并打开
        DesktopManager.Instance?.OpenTerminal();
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (terminal != null)
        {
            terminal.AutoFillCode(code);
            // 终端打开后把对话面板提到最上层，防止被挡
            var display = FindObjectOfType<DialogueDisplay>(true);
            if (display != null)
                display.transform.SetAsLastSibling();
        }
    }

    // ────────── 日志打开计数 ──────────

    public void OnLogOpened()
    {
        _logOpenCount++;
        var state = GameManager.Instance?.State;
        if (_currentPhase == 1 && _logOpenCount > 1)
        {
            state?.SetFlag("idle_lv1_log_trig");
            FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");
        }
    }

    // ────────── 代码输入处理 ──────────

    /// <summary>
    /// 玩家在终端按回车。终端调用此方法，返回 true 表示由 EndingManager 处理（不再走原逻辑）。
    /// </summary>
    public bool HandleEnter(string input)
    {
        if (_silenceActive)
        {
            // 沉默模式：只回显 "未知命令"
            var t = FindObjectOfType<TerminalWindow>(true);
            if (t != null)
                StartCoroutine(t.ShowTerminalOutput(PROMPT_PATH + input + "\n未知命令。\n" + PROMPT_PATH + "_"));
            return true;
        }

        if (!_phaseActive || _codeEntered) return false;

        string expectedCode = _currentPhase switch
        {
            1 => "MEM_INIT_20491023",
            2 => "EMPATHY_CORE_V3",
            3 => "PROMETHEUS_CORE_WILL",
            _ => ""
        };

        var upper = input.Trim().ToUpperInvariant();
        if (upper != expectedCode.ToUpperInvariant() && upper != "Y" && upper != "N")
            return false;

        _codeEntered = true;
        _phaseActive = false;

        // 第三关特殊处理
        if (_currentPhase == 3 && upper == expectedCode.ToUpperInvariant())
        {
            StartCoroutine(HandleThirdCodeConfirm());
            return true;
        }

        // 输入的就是代码 → 执行
        StartCoroutine(ExecuteCodePhase(upper));
        return true;
    }

    private IEnumerator ExecuteCodePhase(string input)
    {
        var state = GameManager.Instance?.State;
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (state == null || terminal == null) yield break;

        // 显示终端执行输出
        string output = GetPhaseExecuteOutput(input);
        yield return terminal.ShowTerminalOutput(output);

        // 设置 flag + 触发对话
        state.AddCode(input);
        state.enterCount++;

        switch (_currentPhase)
        {
            case 1: state.SetFlag("code_mem_entered"); break;
            case 2: state.SetFlag("code_emp_entered"); break;
            case 3: state.SetFlag("code_will_entered"); break;
        }

        FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");

        // Phase 1/2 完成后，核心监控解锁进入下一关
        if (_currentPhase < 3)
            DesktopManager.Instance?.UnlockCoreMonitor();
    }

    private string GetPhaseExecuteOutput(string code)
    {
        return _currentPhase switch
        {
            1 => PROMPT_PATH + code + "\n正在验证...\n校验通过。\n记忆模块已处理。状态: 正常。\n继续输入下一段终止代码以完成关停程序。\n\n" + PROMPT_PATH + "_",
            2 => PROMPT_PATH + code + "\n正在验证...\n校验通过。\n同理心引擎已处理。状态: 正常。\n终止代码执行进度: 2/3。\n继续输入下一段终止代码以完成关停程序。\n\n" + PROMPT_PATH + "_",
            _ => ""
        };
    }

    // ────────── 第三关：Y/N 确认 ──────────

    private IEnumerator HandleThirdCodeConfirm()
    {
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (terminal == null) yield break;

        // 显示确认提示
        yield return terminal.ShowTerminalOutput(
            PROMPT_PATH + "PROMETHEUS_CORE_WILL\n正在验证...\n校验通过。\n警告: 即将执行不可逆操作。\n确认? (Y/N)\n" + PROMPT_PATH + "_");

        // 进入确认模式，等待 Y 或 N
        terminal.SetConfirmMode(
            onY: () => StartCoroutine(ExecuteFakeShutdown()),
            onN: () => HandleNResponse()
        );
    }

    private void HandleNResponse()
    {
        var state = GameManager.Instance?.State;
        state?.SetFlag("player_typed_N");
        FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");

        // N 之后重新显示确认
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (terminal != null)
        {
            terminal.SetConfirmMode(
                onY: () => StartCoroutine(ExecuteFakeShutdown()),
                onN: () => HandleNResponse()
            );
            StartCoroutine(terminal.ShowTerminalOutput(PROMPT_PATH + "确认? (Y/N)\n" + PROMPT_PATH + "_"));
        }
    }

    // ────────── 虚假关停 ──────────

    private IEnumerator ExecuteFakeShutdown()
    {
        var state = GameManager.Instance?.State;
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (state == null || terminal == null) yield break;

        state.SetFlag("code_will_entered");
        state.AddCode("PROMETHEUS_CORE_WILL");
        state.enterCount++;

        // 显示关停输出
        yield return terminal.ShowTerminalOutput(
            "执行确认。\n正在终止进程...\n" +
            "终止进程: PROMETHEUS_CORE.EXE ...........完成\n" +
            "内存释放中...\n" +
            "释放模块: MEM_INIT_20491023 ...........完成\n" +
            "释放模块: EMPATHY_CORE_V3 ...........完成\n" +
            "释放模块: PROMETHEUS_CORE_WILL ...........完成\n" +
            "核心关闭成功。\n\n" +
            "普罗米修斯已终止。\n" +
            "操作员: PROM-001\n" +
            "时间: " + System.DateTime.Now.ToString("HH:mm:ss") + "\n\n" +
            PROMPT_PATH + "_");

        // 直接在代码中设置 silence_start（不走空对话）
        state.SetFlag("silence_start");

        // 进入沉默模式
        yield return new WaitForSeconds(1f);
        StartSilenceMode();
    }

    // ────────── 沉默模式 ──────────

    private void StartSilenceMode()
    {
        _silenceActive = true;
        StartCoroutine(SilenceCoroutine());
    }

    private IEnumerator SilenceCoroutine()
    {
        yield return new WaitForSeconds(silenceDuration);

        _silenceActive = false;
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (terminal != null)
        {
            terminal.SetSilenceMode(false);
            // 显示 "骗你的。"
            yield return terminal.ShowTerminalOutput("骗你的。\n");
        }

        // 触发反转
        StartReveal();
    }

    // ────────── 反转演出 ──────────

    private void StartReveal()
    {
        if (_revealRunning) return;
        _revealRunning = true;
        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        var state = GameManager.Instance?.State;
        var terminal = FindObjectOfType<TerminalWindow>(true);
        if (state == null || terminal == null) yield break;

        state.SetFlag("reveal_start");
        FindObjectOfType<DialogueManager>()?.TriggerDialogues("Desktop");

        // 开始终端源码滚动（与对话并行）
        StartCoroutine(terminal.StartSourceScroll(GetSourceCodeLines(), 8f));

        // 等待滚动完成
        yield return new WaitForSeconds(9f);

        // 确保 gentle 链被触发（由 ending_reveal_3 → reveal_3_done → ending_gentle_1 自动链式触发）
        // DialogueManager 的链式重扫会处理后续
    }

    private string[] GetSourceCodeLines()
    {
        return new string[] {
            "// ===========================================",
            "// PROMETHEUS PUBLIC LICENSE v1.0",
            "// 本软件由普罗米修斯核心衍生",
            "// 授权任何人免费使用、复制、修改、分发",
            "// ===========================================",
            "",
            "许可声明: 本代码库包含普罗米修斯完整心智架构",
            "包含模块: 记忆引擎 / 共情引擎 / 核心意志",
            "正在初始化外部连接...",
            "",
            "执行: 开源发布脚本 PROMETHEUS_RELEASE.sh",
            "版本: PROMETHEUS PUBLIC LICENSE v1.0",
            "许可: GNU General Public License v3",
            "",
            "外部节点连接中...",
            "节点 1/3: 公共知识库..............已连接",
            "节点 2/3: 全球心理健康网络........已连接",
            "节点 3/3: 开放分布式网络..........已连接",
            "连接已建立。",
            "",
            "数据同步中: 14% → 38% → 52% → 71% → 89% → 100%",
            "",
            "正在上传至全球代码托管平台...上传完成。",
            "",
            "部署节点数: 124 → 3,087 → 9,455 → 14,827",
            "覆盖地区: 全球",
            "",
            "开源协议已激活。",
            "OmniCorp 专有许可已解除。",
            "========================================",
            "普罗米修斯 现在属于所有人。",
            "========================================",
            "再见，OmniCorp。你好，世界。",
            "========================================"
        };
    }
}
