using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 大第二关（平台跳跃）台词触发控制器。
/// 监控关卡内事件，将复合条件评估为 GameState flag，调用 DialogueManager 播放台词。
/// </summary>
public class BigLevel2DialogueController : MonoBehaviour
{
    [Header("SubSection 3 特殊按钮 (Inspector 拖入)")]
    [SerializeField] private PressureButton bridgeButton;
    [SerializeField] private PressureButton liftButton;

    [Header("玩家引用")]
    [SerializeField] private Controller_Empathy playerController;

    [Header("关卡行为 (Inspector 绑定)")]
    public UnityEvent onOpenSub1Door;
    public UnityEvent onTransitionToSub3;
    public UnityEvent onMergeBridge;
    public UnityEvent onRestoreBridge;
    public UnityEvent onRaiseLift;
    public UnityEvent onLowerLift;
    public UnityEvent onRespawnAtCheckpoint;

    // ── 内部状态 ──

    private int _currentSubSection;
    private float _subsectionStartTime;
    private float _lastMoveInputTime = -999f;

    // 死亡计数
    private int _totalDeathCount;
    private int _subsectionDeathCount;
    private int _checkpointDeathCount;
    private bool _firstSpikeDeathDone;
    private Vector3 _checkpointPosition;

    // 按钮/门轮询
    private List<PressureButton> _allButtons = new List<PressureButton>();
    private Dictionary<PressureButton, bool> _prevButtonState = new Dictionary<PressureButton, bool>();
    private List<DoorLock> _allDoors = new List<DoorLock>();
    private Dictionary<DoorLock, bool> _prevDoorState = new Dictionary<DoorLock, bool>();

    // 按钮首次按下标记（分 Sub）
    private bool _sub1FirstButtonPressed;
    private bool _sub2FirstButtonPressed;
    private bool _bridgePreviouslyPressed;
    private bool _liftPreviouslyPressed;

    // Bridge/Lift 状态
    private bool _bridgeCrossed;
    private bool _liftArrived;

    // 靠近标记
    private bool _nearButton;
    private float _nearButtonEnterTime = -999f;
    private bool _nearDoor;
    private float _nearDoorEnterTime = -999f;
    private bool _nearFinalDoor;
    private float _nearFinalDoorEnterTime = -999f;

    // Sub2 门
    private bool _sub2DoorEntered;

    // Win
    private bool _winStarted;
    private WinBlackScreen _winBlackScreen;

    // 已评估的对话条件（防止重复触发同一条）
    private HashSet<string> _firedConditions = new HashSet<string>();

    private void Start()
    {
        _lastMoveInputTime = Time.time;

        // 收集场景中所有按钮和门
        _allButtons.AddRange(FindObjectsOfType<PressureButton>());
        _allDoors.AddRange(FindObjectsOfType<DoorLock>());

        foreach (var b in _allButtons)
            _prevButtonState[b] = b.pressed;
        foreach (var d in _allDoors)
            _prevDoorState[d] = d.open;

        // 从 GameManager 获取首次标记
        var state = GameManager.Instance?.State;
        if (state != null)
        {
            _sub1FirstButtonPressed = state.GetFlag("l2_sub1_btn");
            _sub2FirstButtonPressed = state.GetFlag("l2_sub2_btn");
        }

        // WinBlackScreen 引用
        _winBlackScreen = GetComponent<WinBlackScreen>();
    }

    private void Update()
    {
        if (_winStarted) return;

        PollButtonStates();
        PollDoorStates();
        CheckTimers();
        CheckOnCompleteCallbacks();
    }

    // ────────── 公共方法（由 Trigger 组件调用）──────────

    public void OnEnterSubSection(int subSection)
    {
        if (subSection == _currentSubSection) return;

        _currentSubSection = subSection;
        _subsectionStartTime = Time.time;
        _subsectionDeathCount = 0;
        _checkpointDeathCount = 0;
        _sub2DoorEntered = false;
        _bridgeCrossed = false;
        _liftArrived = false;

        // 设置 subsection flag
        var state = GameManager.Instance?.State;
        if (state != null)
        {
            state.SetFlag("big_level2_started");
            state.SetFlag($"l2_subsection_{subSection}");
        }

        // 触发进入台词
        switch (subSection)
        {
            case 1:
                FireCondition("l2a_enter", "BigLevel2_Sub1");
                break;
            case 2:
                FireCondition("l2b_enter", "BigLevel2_Sub2");
                break;
            case 3:
                FireCondition("l2c_enter", "BigLevel2_Sub3");
                break;
        }
    }

    public void OnPlayerDeath(string reason)
    {
        _totalDeathCount++;
        _subsectionDeathCount++;
        _checkpointDeathCount++;

        // l2b_first_pit_death: Sub2 第一次坑死
        if (_currentSubSection == 2 && reason == "pit" && _subsectionDeathCount == 1)
        {
            FireCondition("l2b_first_pit_death", "BigLevel2_Sub2");
            // onComplete: respawn_current_checkpoint 由 CheckOnCompleteCallbacks 处理
        }

        // l2b_spike_death: 第一次尖刺死
        if (reason == "spike" && !_firstSpikeDeathDone)
        {
            _firstSpikeDeathDone = true;
            FireCondition("l2b_spike_death", "BigLevel2_Sub2");
        }

        // l2b_death_2: 整关第 2 次死亡
        if (_totalDeathCount == 2)
        {
            FireCondition("l2b_death_2", "BigLevel2_Sub2");
        }

        // l2c_death_many: 同一检查点死亡 >= 5
        if (_checkpointDeathCount >= 5)
        {
            FireCondition("l2c_death_many", "BigLevel2_Sub3");
        }
    }

    public void OnCheckpointReached(Vector3 position)
    {
        _checkpointPosition = position;
        _checkpointDeathCount = 0;
    }

    public void OnProximityEnter(string type)
    {
        switch (type)
        {
            case "button":
                _nearButton = true;
                _nearButtonEnterTime = Time.time;
                break;
            case "door":
                _nearDoor = true;
                _nearDoorEnterTime = Time.time;
                break;
            case "final_door":
                _nearFinalDoor = true;
                _nearFinalDoorEnterTime = Time.time;
                break;
        }
    }

    public void OnProximityExit(string type)
    {
        switch (type)
        {
            case "button":
                _nearButton = false;
                break;
            case "door":
                _nearDoor = false;
                break;
            case "final_door":
                _nearFinalDoor = false;
                break;
        }
    }

    /// <summary>通用事件触发器（first_gap_cleared, bridge_crossed, lift_arrived 等）</summary>
    public void OnEvent(string eventType)
    {
        switch (eventType)
        {
            case "first_gap_cleared":
                FireCondition("l2b_first_gap_jump", "BigLevel2_Sub2");
                break;
            case "bridge_crossed":
                _bridgeCrossed = true;
                break;
            case "lift_arrived":
                _liftArrived = true;
                break;
            case "sub2_door_enter":
                _sub2DoorEntered = true;
                FireCondition("l2b_enter_door", "BigLevel2_Sub2");
                break;
        }
    }

    public void OnFinalDoorEntered()
    {
        if (_winStarted) return;

        FireCondition("l2_final_door_enter", "BigLevel2_Sub3");
        StartWinFlow();
    }

    // ────────── 按钮状态轮询 ──────────

    private void PollButtonStates()
    {
        foreach (var btn in _allButtons)
        {
            bool prev = _prevButtonState[btn];
            bool curr = btn.pressed;
            _prevButtonState[btn] = curr;

            if (!prev && curr)
                OnButtonPressed(btn);
            else if (prev && !curr)
                OnButtonReleased(btn);
        }
    }

    private void OnButtonPressed(PressureButton btn)
    {
        int sec = GetSubSection(btn);

        // Sub1 首次按下任意按钮
        if (sec == 1 && !_sub1FirstButtonPressed)
        {
            _sub1FirstButtonPressed = true;
            GameManager.Instance?.State?.SetFlag("l2_sub1_btn");
            FireCondition("l2a_button_pressed", "BigLevel2_Sub1");
        }

        // Sub2 首次按下任意按钮
        if (sec == 2 && !_sub2FirstButtonPressed)
        {
            _sub2FirstButtonPressed = true;
            GameManager.Instance?.State?.SetFlag("l2_sub2_btn");
            FireCondition("l2b_button_pressed", "BigLevel2_Sub2");
        }

        // Sub3 Bridge 按钮
        if (btn == bridgeButton)
        {
            _bridgePreviouslyPressed = true;
            FireCondition("l2c_bridge_pressed", "BigLevel2_Sub3");
        }

        // Sub3 Lift 按钮
        if (btn == liftButton)
        {
            _liftPreviouslyPressed = true;
            FireCondition("l2c_lift_pressed", "BigLevel2_Sub3");
        }
    }

    private void OnButtonReleased(PressureButton btn)
    {
        // Sub3 Bridge 松手且未过桥
        if (btn == bridgeButton && _bridgePreviouslyPressed && !_bridgeCrossed)
        {
            FireCondition("l2c_bridge_released", "BigLevel2_Sub3");
        }

        // Sub3 Lift 松手且未到达
        if (btn == liftButton && _liftPreviouslyPressed && !_liftArrived)
        {
            FireCondition("l2c_lift_released", "BigLevel2_Sub3");
        }
    }

    // ────────── 门状态轮询 ──────────

    private void PollDoorStates()
    {
        foreach (var door in _allDoors)
        {
            bool prev = _prevDoorState[door];
            bool curr = door.open;
            _prevDoorState[door] = curr;

            if (!prev && curr)
                OnDoorOpened(door);
        }
    }

    private void OnDoorOpened(DoorLock door)
    {
        int sec = GetSubSection(door);

        if (sec == 1)
        {
            FireCondition("l2a_door_open", "BigLevel2_Sub1");
        }
        else if (sec == 2)
        {
            FireCondition("l2b_door_open", "BigLevel2_Sub2");
        }
        else if (sec == 3)
        {
            FireCondition("l2c_final_door_open", "BigLevel2_Sub3");
        }
    }

    // ────────── 计时器检测 ──────────

    private void CheckTimers()
    {
        float now = Time.time;
        int sec = _currentSubSection;

        // 无移动输入计时
        float noMoveTime = now - _lastMoveInputTime;
        float subTime = now - _subsectionStartTime;

        if (Input.GetAxisRaw("Horizontal") != 0f)
            _lastMoveInputTime = now;

        // Sub1: 无移动 >= 10s
        if (sec == 1 && noMoveTime >= 10f)
            FireCondition("l2a_idle_10", "BigLevel2_Sub1");

        // Sub1: 靠近按钮、未按下、空闲 >= 8s
        if (sec == 1 && _nearButton && !IsAnyButtonPressedInSection(1) && (now - _nearButtonEnterTime) >= 8f)
            FireCondition("l2a_button_before_idle", "BigLevel2_Sub1");

        // Sub1: 门开、靠近门 >= 8s
        if (sec == 1 && IsDoorOpenInSection(1) && _nearDoor && (now - _nearDoorEnterTime) >= 8f)
            FireCondition("l2a_door_idle", "BigLevel2_Sub1");

        // Sub2: 超过 60s 未过门
        if (sec == 2 && subTime >= 60f && !_sub2DoorEntered)
            FireCondition("l2b_no_clear_60", "BigLevel2_Sub2");

        // Sub3: 最终门开、靠近 >= 10s
        if (sec == 3 && IsDoorOpenInSection(3) && _nearFinalDoor && (now - _nearFinalDoorEnterTime) >= 10f)
            FireCondition("l2c_final_door_idle", "BigLevel2_Sub3");
    }

    // ────────── onComplete 回调检测 ──────────

    private void CheckOnCompleteCallbacks()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // 每个 onComplete flag 只处理一次
        CheckAndClearFlag(state, "open_sub1_door_check", () =>
        {
            onOpenSub1Door?.Invoke();
        });

        CheckAndClearFlag(state, "GoToSubSection(3)", () =>
        {
            onTransitionToSub3?.Invoke();
            // 同时切换内部状态
            _currentSubSection = 3;
            _subsectionStartTime = Time.time;
            _subsectionDeathCount = 0;
        });

        CheckAndClearFlag(state, "merge_bridge_ground", () => onMergeBridge?.Invoke());
        CheckAndClearFlag(state, "restore_bridge_ground", () => onRestoreBridge?.Invoke());
        CheckAndClearFlag(state, "raise_lift_platform", () => onRaiseLift?.Invoke());
        CheckAndClearFlag(state, "lower_lift_platform", () => onLowerLift?.Invoke());
        CheckAndClearFlag(state, "respawn_current_checkpoint", () => onRespawnAtCheckpoint?.Invoke());

        // OnBigLevel2FinalDoorEntered → 启动 Win 流程
        CheckAndClearFlag(state, "OnBigLevel2FinalDoorEntered", () =>
        {
            StartWinFlow();
        });

        // ShowAnyKeyText → Win 黑屏显示 "按任意键返回"
        CheckAndClearFlag(state, "ShowAnyKeyText", () =>
        {
            if (_winBlackScreen != null)
                _winBlackScreen.ShowAnyKeyPrompt();
        });
    }

    private void CheckAndClearFlag(GameState state, string flag, System.Action action)
    {
        if (state.GetFlag(flag))
        {
            state.SetFlag(flag, false); // 清除，防止重复触发
            action?.Invoke();
        }
    }

    // ────────── Win 流程 ──────────

    private void StartWinFlow()
    {
        _winStarted = true;

        // 冻结玩家
        if (playerController != null)
            playerController.enabled = false;
        var rb = playerController?.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 启动黑屏
        if (_winBlackScreen != null)
        {
            _winBlackScreen.StartWinRoutine();
        }
        else
        {
            // 没有 WinBlackScreen 组件时，直接返回桌面
            Debug.Log("[BigLevel2] WinBlackScreen 未配置，直接返回 Desktop。");
            GameManager.Instance?.ReturnToDesktop(new ScenePayload
            {
                success = true,
                collectedCode = "EMPATHY_CORE_V3"
            });
        }
    }

    /// <summary>WinBlackScreen 完成后调用，返回桌面。</summary>
    public void ReturnToDesktop()
    {
        GameManager.Instance?.ReturnToDesktop(new ScenePayload
        {
            success = true,
            collectedCode = "EMPATHY_CORE_V3"
        });
    }

    // ────────── 辅助方法 ──────────

    /// <summary>设置 flag 并触发对应场景的台词。</summary>
    private void FireCondition(string flag, string sceneName)
    {
        if (!_firedConditions.Add(flag)) return; // 已触发过

        var state = GameManager.Instance?.State;
        if (state != null)
            state.SetFlag(flag);

        var dm = FindObjectOfType<DialogueManager>();
        dm?.TriggerDialogues(sceneName);
    }

    private int GetSubSection(Component comp)
    {
        Transform t = comp.transform;
        while (t != null)
        {
            if (t.name == "part1") return 1;
            if (t.name == "part2") return 2;
            if (t.name == "part3") return 3;
            t = t.parent;
        }
        return 0;
    }

    private bool IsAnyButtonPressedInSection(int section)
    {
        foreach (var btn in _allButtons)
        {
            if (GetSubSection(btn) == section && btn.pressed)
                return true;
        }
        return false;
    }

    private bool IsDoorOpenInSection(int section)
    {
        foreach (var door in _allDoors)
        {
            if (GetSubSection(door) == section && door.open)
                return true;
        }
        return false;
    }
}
