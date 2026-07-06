using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 大第二关 Win 黑屏流程。
/// 创建全屏黑底 → 显示 EMPATHY_CORE_V3 → 打字机台词 → "按任意键返回" → 返回桌面。
/// 由 BigLevel2DialogueController 在 OnBigLevel2FinalDoorEntered 时启动。
/// </summary>
public class WinBlackScreen : MonoBehaviour
{
    [Header("代码显示")]
    [SerializeField] private string codeText = "EMPATHY_CORE_V3";
    [SerializeField] private float codeFontSize = 36f;

    [Header("台词")]
    [SerializeField] private string line1 = "这就是第二段核心代码。";
    [SerializeField] private string line2 = "记住它。";

    [Header("速度")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float codeDisplayPause = 0.8f;
    [SerializeField] private float typewriterSpeed = 0.04f;
    [SerializeField] private float blinkInterval = 0.6f;

    private Canvas _canvas;
    private TMP_Text _codeText;
    private TMP_Text _dialogueText;
    private TMP_Text _anyKeyText;
    private UnityEngine.UI.Image _overlay;
    private bool _anyKeyActive;
    private bool _routineComplete;

    private BigLevel2DialogueController _controller;

    private void Awake()
    {
        _controller = GetComponent<BigLevel2DialogueController>();
    }

    public void StartWinRoutine()
    {
        if (_routineComplete) return;
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        // 1. 构建 UI
        BuildUI();

        // 2. 淡入黑屏
        yield return StartCoroutine(FadeBlack(0f, 1f, fadeDuration));

        // 3. 显示代码（打字机效果）
        yield return StartCoroutine(TypeCode(codeText));

        // 4. 等待 0.8s
        yield return new WaitForSeconds(codeDisplayPause);

        // 5. 显示台词 1
        yield return StartCoroutine(TypeDialogue(line1));
        yield return new WaitForSeconds(0.6f);

        // 6. 显示台词 2
        yield return StartCoroutine(TypeDialogue(line2));
        yield return new WaitForSeconds(0.8f);

        // 7. 显示"按任意键返回"（呼吸闪烁）
        ShowAnyKeyPrompt();
    }

    public void ShowAnyKeyPrompt()
    {
        if (_anyKeyActive) return;
        _anyKeyActive = true;
        StartCoroutine(AnyKeyLoop());
    }

    private void BuildUI()
    {
        // 创建 Canvas
        var canvasGo = new GameObject("WinBlackCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 黑底
        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(canvasGo.transform, false);
        _overlay = overlayGo.AddComponent<UnityEngine.UI.Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0f);
        var overlayRt = _overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        // 字体
        TMP_FontAsset font = null;
        try { font = Resources.Load<TMP_FontAsset>("simhei SDF"); } catch { }

        // 代码文本（居中大字）
        _codeText = CreateTMPText("CodeText", canvasGo.transform, font, codeFontSize,
            new Vector2(0.5f, 0.55f), TextAlignmentOptions.Center, "");
        _codeText.color = new Color(1f, 1f, 1f, 0f);

        // 台词文本（代码下方）
        _dialogueText = CreateTMPText("DialogueText", canvasGo.transform, font, 18f,
            new Vector2(0.5f, 0.35f), TextAlignmentOptions.Center, "");
        _dialogueText.color = new Color(1f, 1f, 1f, 0f);

        // "按任意键返回"文本（底部）
        _anyKeyText = CreateTMPText("AnyKeyText", canvasGo.transform, font, 14f,
            new Vector2(0.5f, 0.15f), TextAlignmentOptions.Center, "按任意键返回");
        _anyKeyText.color = new Color(1f, 1f, 1f, 0f);
    }

    private TMP_Text CreateTMPText(string name, Transform parent, TMP_FontAsset font,
        float fontSize, Vector2 anchor, TextAlignmentOptions alignment, string initialText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = initialText;
        text.color = new Color(1f, 1f, 1f, 0f);

        var rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(600f, 100f);

        // 确保 TMP 字体可读
        if (font != null)
        {
            text.fontSharedMaterial = font.material;
        }

        return text;
    }

    private IEnumerator FadeBlack(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));
            yield return null;
        }
        _overlay.color = new Color(0f, 0f, 0f, to);
    }

    private IEnumerator TypeCode(string fullText)
    {
        _codeText.color = new Color(1f, 1f, 1f, 1f);

        // 逐字出现 + 轻微闪烁
        for (int i = 0; i <= fullText.Length; i++)
        {
            _codeText.text = fullText.Substring(0, i);
            // 闪烁最后一字符
            if (i > 0 && i == fullText.Length)
            {
                float flickerTime = 0f;
                while (flickerTime < 0.4f)
                {
                    flickerTime += Time.unscaledDeltaTime;
                    float alpha = 0.6f + Mathf.Abs(Mathf.Sin(flickerTime * 20f)) * 0.4f;
                    _codeText.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
            }
            else if (i < fullText.Length)
            {
                yield return new WaitForSecondsRealtime(typewriterSpeed * 0.5f);
            }
        }
        _codeText.color = Color.white;
    }

    private IEnumerator TypeDialogue(string fullText)
    {
        _dialogueText.color = new Color(1f, 1f, 1f, 1f);
        _dialogueText.text = "";

        for (int i = 0; i <= fullText.Length; i++)
        {
            _dialogueText.text = fullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
    }

    private IEnumerator AnyKeyLoop()
    {
        // 显示文本
        _anyKeyText.color = new Color(1f, 1f, 1f, 1f);

        // 呼吸闪烁
        float blinkTimer = 0f;
        while (!Input.anyKeyDown)
        {
            blinkTimer += Time.unscaledDeltaTime;
            float alpha = 0.3f + Mathf.Abs(Mathf.Sin(blinkTimer * Mathf.PI / blinkInterval)) * 0.7f;
            _anyKeyText.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        // 消耗这一帧的所有输入，防止带到 Desktop
        Input.ResetInputAxes();

        _routineComplete = true;

        // 返回桌面
        _controller?.ReturnToDesktop();
    }
}
