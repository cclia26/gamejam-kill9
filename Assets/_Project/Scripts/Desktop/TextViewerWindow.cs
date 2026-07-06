using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 文本查看窗口 — 用于显示 readme.txt、系统日志、董事会文件等。
/// </summary>
public class TextViewerWindow : DraggableWindow
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private ScrollRect scrollRect;

    public void SetContent(string fileName, string content)
    {
        windowTitle = fileName;
        if (titleText != null)
            titleText.text = fileName;
        if (contentText != null)
            contentText.text = content;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        // 更新任务栏标签
        if (_taskbarEntry != null && _taskbarEntry.label != null)
            _taskbarEntry.label.text = fileName;

        Open();
    }
}
