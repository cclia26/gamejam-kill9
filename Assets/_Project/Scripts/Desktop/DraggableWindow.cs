using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖拽窗口基类 — 提供窗口拖动、打开/关闭行为。
/// </summary>
public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] protected RectTransform headerBar;
    [SerializeField] protected Button closeButton;
    protected Canvas canvas;

    public System.Action onClosed;

    protected virtual void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (headerBar != null)
            transform.localPosition += (Vector3)eventData.delta / canvas.scaleFactor;
    }
}
