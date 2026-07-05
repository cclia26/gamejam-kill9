using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖拽窗口基类 — 拖动标题栏、关闭、最小化、任务栏集成。
/// </summary>
public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Window")]
    [SerializeField] protected RectTransform headerBar;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button minimizeButton;
    [SerializeField] private Sprite windowFrameSprite;
    protected Canvas canvas;

    public System.Action onClosed;
    public string windowTitle = "窗口";
    public Sprite taskbarIcon;

    protected TaskbarEntry _taskbarEntry;
    private bool _taskbarRegistered;

    protected virtual void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(Minimize);

        ApplyWindowFrame();
        RegisterToTaskbar();
    }

    private void ApplyWindowFrame()
    {
        if (windowFrameSprite == null) return;

        var frameGo = new GameObject("WindowFrame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(transform, false);
        frameGo.transform.SetAsFirstSibling(); // 最底层，不挡文字

        // 有自定义窗口图片时，隐藏真实按钮（包括文字），图片上的绘制按钮 + 点击穿透响应
        if (closeButton != null)
        {
            var g = closeButton.targetGraphic;
            if (g != null) g.color = new Color(1, 1, 1, 0);
            foreach (var t in closeButton.GetComponentsInChildren<TMPro.TMP_Text>())
                t.color = new Color(1, 1, 1, 0);
        }
        if (minimizeButton != null)
        {
            var g = minimizeButton.targetGraphic;
            if (g != null) g.color = new Color(1, 1, 1, 0);
            foreach (var t in minimizeButton.GetComponentsInChildren<TMPro.TMP_Text>())
                t.color = new Color(1, 1, 1, 0);
        }

        var img = frameGo.GetComponent<Image>();
        img.sprite = windowFrameSprite;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;    // 不拦截点击，穿透到按钮

        var rt = frameGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void RegisterToTaskbar()
    {
        if (_taskbarRegistered) return;
        _taskbarEntry = Taskbar.Instance?.RegisterWindow(this, windowTitle, taskbarIcon);
        _taskbarRegistered = _taskbarEntry != null;
    }

    protected virtual void OnDestroy()
    {
        _taskbarEntry?.OnWindowClosed();
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RegisterToTaskbar();
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
        onClosed?.Invoke();
        _taskbarEntry?.OnWindowClosed();
        _taskbarEntry = null;
        _taskbarRegistered = false;
    }

    public virtual void Minimize()
    {
        gameObject.SetActive(false);
    }

    public virtual void Restore()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (headerBar != null)
            transform.localPosition += (Vector3)eventData.delta / canvas.scaleFactor;
    }
}
