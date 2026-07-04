using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 底部任务栏 — 显示系统时间 + 已打开窗口的入口按钮。
/// </summary>
public class Taskbar : MonoBehaviour
{
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private RectTransform entriesParent;
    [SerializeField] private GameObject entryPrefab;

    public static Taskbar Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (clockText != null)
            clockText.text = DateTime.Now.ToString("HH:mm");
    }

    /// <summary>注册一个窗口，在任务栏创建对应按钮。</summary>
    public TaskbarEntry RegisterWindow(DraggableWindow window, string title, Sprite icon = null)
    {
        if (entryPrefab == null)
        {
            Debug.LogWarning("[Taskbar] entryPrefab 未设置，无法创建任务栏按钮。请在 Editor 中将 TaskbarEntryPrefab 拖到 Taskbar 的 Entry Prefab 槽位。");
            return null;
        }
        var parent = entriesParent != null ? entriesParent : (RectTransform)transform;
        var go = Instantiate(entryPrefab, parent);
        var entry = go.GetComponent<TaskbarEntry>();
        if (entry == null)
        {
            Debug.LogWarning("[Taskbar] entryPrefab 缺少 TaskbarEntry 组件，请添加。");
            Destroy(go); // 清理孤儿按钮
            return null;
        }
        entry.Init(window, title, icon);
        Debug.Log($"[Taskbar] 注册窗口: {title} (父节点共 {parent.childCount} 个子对象)");
        return entry;
    }
}
