using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖拽窗口基类 — 拖动标题栏、关闭、最小化、任务栏集成。
/// </summary>
public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] protected RectTransform headerBar;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button minimizeButton;
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

        // 注册到任务栏（仅一次）
        RegisterToTaskbar();
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
