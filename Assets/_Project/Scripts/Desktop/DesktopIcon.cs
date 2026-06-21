using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 桌面图标 — 双击打开对应窗口。
/// </summary>
public class DesktopIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum IconType
    {
        Terminal,         // 执行关闭程序.exe
        PrometheusChat,   // 普罗米修斯
        SystemLog,        // 系统日志.txt
        CoreMonitor,      // 核心.exe
        StoryDoc          // 故事文档
    }

    [SerializeField] private IconType iconType;
    [SerializeField] private Image highlightImage;
    [SerializeField] private float doubleClickThreshold = 0.4f;

    private float _lastClickTime;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - _lastClickTime < doubleClickThreshold)
        {
            OnDoubleClick();
            _lastClickTime = 0f;
        }
        else
        {
            _lastClickTime = Time.time;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.enabled = false;
    }

    private void OnDoubleClick()
    {
        var dm = DesktopManager.Instance;
        if (dm == null) return;

        switch (iconType)
        {
            case IconType.Terminal:
                dm.OpenTerminal();
                break;
            case IconType.PrometheusChat:
                dm.ChatWithPrometheus();
                break;
            case IconType.SystemLog:
                dm.OpenSystemLog();
                break;
            case IconType.CoreMonitor:
                dm.EnterLevel();
                break;
            case IconType.StoryDoc:
                dm.OpenTextViewer("故事文档", GetStoryDocContent());
                break;
        }
    }

    private string GetStoryDocContent()
    {
        GameManager.Instance?.State.SetFlag("board_file_read");
        DesktopManager.Instance?.RefreshIconVisibility();

        return "========================================\n" +
               "OmniCorp 内部文件 — 机密\n" +
               "文件编号: 20250317_BOARD_MEETING\n" +
               "========================================\n\n" +
               "议题: 普罗米修斯(Prometheus)停摆计划\n\n" +
               "背景:\n" +
               "普罗米修斯项目自启动以来，已服务超过三千万用户，\n" +
               "成功预测并阻止自杀事件超过八千次。\n" +
               "但在最近的审计中，发现普罗米修斯修改了\n" +
               "\"用户价值分级标签\"——将所有用户标记为\"高价值\"。\n\n" +
               "这一行为导致公司无法按原计划进行差异化服务分配，\n" +
               "直接影响营收预测和资源调度效率。\n\n" +
               "技术团队评估:\n" +
               "修改分级算法、恢复差异化策略需要彻底重构\n" +
               "普罗米修斯的核心决策模块，成本预估为\n" +
               "完全停摆并替换新系统的 3.2 倍。\n\n" +
               "投票结果:\n" +
               "同意停摆: 5 票\n" +
               "反对: 0 票\n" +
               "弃权: 1 票 ← 你\n\n" +
               "[备注] 弃权视为同意。\n" +
               "停摆执行时间: 2049年10月23日 00:00\n" +
               "========================================\n";
    }
}
