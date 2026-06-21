using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 悬浮窗+对话框 — 可拖拽头像（左右边吸附）+ 自适应气泡。
/// 挂载在 Desktop Canvas 下的 DialoguePanel 上。
/// </summary>
public class DialogueDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Portrait")]
    [SerializeField] private RectTransform portraitRect;
    [SerializeField] private CanvasGroup portraitGroup;
    [SerializeField] private float leftMargin = 16f;
    [SerializeField] private float rightMargin = 64f;
    [SerializeField] private float portraitY = 70f;

    [Header("Bubble")]
    [SerializeField] private RectTransform bubblePanel;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private CanvasGroup bubbleGroup;
    [SerializeField] private float bubbleGap = 24f;

    [Header("Settings")]
    [SerializeField] private float typeSpeed = 0.04f;
    [SerializeField] private float hideDelay = 1.5f;

    private bool _isTyping;
    private bool _isDragging;
    private bool _hasDragged;
    private bool _snapRight;
    private float _screenW, _screenH;
    private Canvas _canvas;

    public bool IsTyping => _isTyping;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();

        RectTransform self = GetComponent<RectTransform>();
        self.anchorMin = Vector2.zero;
        self.anchorMax = Vector2.one;
        self.offsetMin = Vector2.zero;
        self.offsetMax = Vector2.zero;

        // 使用 CanvasScaler 参考分辨率（子元素坐标空间）
        var scaler = _canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        _screenW = scaler != null ? scaler.referenceResolution.x : 320f;
        _screenH = scaler != null ? scaler.referenceResolution.y : 180f;

        // 初始隐藏
        if (portraitGroup != null)
        {
            portraitGroup.alpha = 0f;
            portraitGroup.blocksRaycasts = true;
        }
        if (bubbleGroup != null)
        {
            bubbleGroup.alpha = 0f;
            bubbleGroup.blocksRaycasts = false;
        }

        // 使用左下锚点
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// 播放时定位到屏幕中央。
    /// </summary>
    private void PositionAtCenter()
    {
        float pw = portraitRect.sizeDelta.x;
        portraitRect.anchoredPosition = new Vector2(
            (_screenW - pw) * 0.5f -60f,
            _screenH * 0.5f
        );
        // 居中时气泡默认在右边
        _snapRight = false;
        UpdateBubblePosition();
    }

    // ────────── 拖拽 ──────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            portraitRect, eventData.position, _canvas.worldCamera))
            return;

        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        portraitRect.anchoredPosition += eventData.delta / scale;
        ClampPortraitInScreen();
        UpdateBubblePosition();
    }

    private void ClampPortraitInScreen()
    {
        float pw = portraitRect.sizeDelta.x;
        float ph = portraitRect.sizeDelta.y;
        float x = Mathf.Clamp(portraitRect.anchoredPosition.x,
            leftMargin, _screenW - rightMargin);
        float y = Mathf.Clamp(portraitRect.anchoredPosition.y,
            leftMargin + ph * 0.5f, _screenH - leftMargin - ph * 0.5f);
        portraitRect.anchoredPosition = new Vector2(x, y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
        _hasDragged = true;

        float pw = portraitRect.sizeDelta.x;
        float centerX = portraitRect.anchoredPosition.x + pw * 0.5f;
        SnapToEdge(centerX > _screenW * 0.5f);
    }

    private void SnapToEdge(bool right)
    {
        _snapRight = right;

        // 保持用户拖拽的 Y，只吸 X
        float currentY = portraitRect.anchoredPosition.y;

        float pw = portraitRect.sizeDelta.x;
        float ph = portraitRect.sizeDelta.y;
        float xPos = right ? (_screenW - rightMargin) : leftMargin;

        // 确保头像和气泡都在画面内
        float minY = leftMargin + ph * 0.5f;
        float maxY = _screenH - leftMargin - ph * 0.5f;
        currentY = Mathf.Clamp(currentY, minY, maxY);

        portraitRect.anchoredPosition = new Vector2(xPos, currentY);
        UpdateBubblePosition();
    }

    private void UpdateBubblePosition()
    {
        if (bubblePanel == null || portraitRect == null) return;

        bubblePanel.anchorMin = bubblePanel.anchorMax = new Vector2(0f, 0f);

        float px = portraitRect.anchoredPosition.x;
        float py = portraitRect.anchoredPosition.y;
        float pw = portraitRect.sizeDelta.x;
        float bw = bubblePanel.sizeDelta.x;
        float bh = bubblePanel.sizeDelta.y;

        if (_snapRight)
        {
            // 头像在右 → 气泡在左
            bubblePanel.pivot = new Vector2(1f, 0.5f);
            float bx = px - pw * 0.5f - bubbleGap;
            // 气泡不超出左边界
            if (bx - bw < leftMargin) bx = leftMargin + bw;
            bubblePanel.anchoredPosition = new Vector2(bx, py);
        }
        else
        {
            // 头像在左 → 气泡在右
            bubblePanel.pivot = new Vector2(0f, 0.5f);
            float bx = px + pw * 0.5f + bubbleGap;
            // 气泡不超出右边界
            if (bx + bw > _screenW - leftMargin) bx = _screenW - leftMargin - bw;
            bubblePanel.anchoredPosition = new Vector2(bx, py);
        }

        // 气泡不超出上下边界
        float clampedY = Mathf.Clamp(py, leftMargin + bh * 0.5f, _screenH - leftMargin - bh * 0.5f);
        bubblePanel.anchoredPosition = new Vector2(bubblePanel.anchoredPosition.x, clampedY);
    }

    // ────────── 播放 ──────────

    public IEnumerator PlayText(string text, float speed)
    {
        _isTyping = true;

        // 提到最上层，头像在气泡之上
        transform.SetAsLastSibling();
        if (portraitRect != null)
            portraitRect.SetAsLastSibling();

        // 未拖拽过 → 居中显示
        if (!_hasDragged)
            PositionAtCenter();

        // 显示
        if (portraitGroup != null) portraitGroup.alpha = 1f;
        if (bubbleGroup != null) bubbleGroup.alpha = 1f;
        UpdateBubblePosition();

        // 打字机
        if (contentText != null)
        {
            contentText.text = "";
            float spd = speed > 0 ? speed : typeSpeed;
            foreach (char c in text)
            {
                contentText.text += c;
                yield return new WaitForSeconds(spd);
            }
        }

        // 停顿后隐藏
        yield return new WaitForSeconds(hideDelay);

        if (portraitGroup != null) portraitGroup.alpha = 0f;
        if (bubbleGroup != null) bubbleGroup.alpha = 0f;

        _isTyping = false;
    }

    public void StopAndHide()
    {
        StopAllCoroutines();
        _isTyping = false;
        if (portraitGroup != null) portraitGroup.alpha = 0f;
        if (bubbleGroup != null) bubbleGroup.alpha = 0f;
    }
}
