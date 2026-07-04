using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务栏窗口入口 — 点击切换窗口显示/隐藏。
/// </summary>
public class TaskbarEntry : MonoBehaviour
{
    public TMP_Text label;
    public Image icon;

    private DraggableWindow _window;
    private Button _button;

    public void Init(DraggableWindow window, string title, Sprite iconSprite)
    {
        _window = window;
        if (label != null) label.text = title;
        if (icon != null && iconSprite != null) icon.sprite = iconSprite;

        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);

        Highlight(false);
    }

    private void OnClick()
    {
        if (_window == null) return;

        if (_window.gameObject.activeSelf)
        {
            // 窗口正在显示 → 最小化
            _window.Minimize();
            Highlight(true);
        }
        else
        {
            // 窗口已隐藏 → 还原
            _window.Restore();
            Highlight(false);
        }
    }

    /// <summary>窗口关闭时调用，销毁入口。</summary>
    public void OnWindowClosed()
    {
        _window = null;
        Destroy(gameObject);
    }

    private void Highlight(bool active)
    {
        if (_button == null) return;
        var colors = _button.colors;
        colors.normalColor = active
            ? new Color(0.35f, 0.5f, 0.7f, 1f)
            : new Color(0.25f, 0.25f, 0.35f, 0.9f);
        _button.colors = colors;
    }
}
