using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Big level 2 win blackout: fade to black, show code, play Prometheus dialogue, click to return.
/// </summary>
public class WinBlackScreen : MonoBehaviour
{
    [Header("Code")]
    [SerializeField] private string codeText = "EMPATHY_CORE_V3";
    [SerializeField] private float codeFontSize = 44f;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float codeDisplayPause = 0.8f;
    [SerializeField] private float typewriterSpeed = 0.04f;
    [SerializeField] private float blinkInterval = 0.6f;

    private Canvas _canvas;
    private TMP_Text _codeText;
    private TMP_Text _anyKeyText;
    private Image _overlay;
    private bool _anyKeyActive;
    private bool _routineComplete;

    private BigLevel2DialogueController _controller;

    private static readonly Color CodeGold = new Color(1f, 0.72f, 0.18f, 1f);
    private static readonly Color PromptGray = new Color(0.72f, 0.72f, 0.72f, 1f);

    private void Awake()
    {
        _controller = GetComponent<BigLevel2DialogueController>();
    }

    public void StartWinRoutine()
    {
        if (_routineComplete || _anyKeyActive)
        {
            return;
        }

        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        BuildUI();

        yield return StartCoroutine(FadeBlack(0f, 1f, fadeDuration));
        yield return StartCoroutine(TypeCode(codeText));
        yield return new WaitForSecondsRealtime(codeDisplayPause);

        yield return StartCoroutine(PlayPrometheusLine("l2_win_code_line1"));
        yield return StartCoroutine(PlayPrometheusLine("l2_win_code_line2"));

        ShowAnyKeyPrompt();
    }

    public void ShowAnyKeyPrompt()
    {
        if (_anyKeyActive)
        {
            return;
        }

        _anyKeyActive = true;
        StartCoroutine(AnyClickLoop());
    }

    private void BuildUI()
    {
        if (_canvas != null)
        {
            return;
        }

        var canvasGo = new GameObject("WinBlackCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 31000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(320f, 180f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(canvasGo.transform, false);
        _overlay = overlayGo.AddComponent<Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0f);
        var overlayRt = _overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("simhei SDF");

        _codeText = CreateTMPText("CodeText", canvasGo.transform, font, codeFontSize,
            new Vector2(0.5f, 0.55f), TextAlignmentOptions.Center, string.Empty);
        _codeText.color = new Color(CodeGold.r, CodeGold.g, CodeGold.b, 0f);

        _anyKeyText = CreateTMPText("AnyClickText", canvasGo.transform, font, 12f,
            new Vector2(0.5f, 0.13f), TextAlignmentOptions.Center, "\u70b9\u51fb\u4efb\u610f\u5904\u8fd4\u56de");
        _anyKeyText.color = new Color(PromptGray.r, PromptGray.g, PromptGray.b, 0f);
    }

    private TMP_Text CreateTMPText(string name, Transform parent, TMP_FontAsset font,
        float fontSize, Vector2 anchor, TextAlignmentOptions alignment, string initialText)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = initialText;
        text.color = Color.clear;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        var rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620f, 80f);
        rt.anchoredPosition = Vector2.zero;

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
        _codeText.color = CodeGold;

        for (int i = 0; i <= fullText.Length; i++)
        {
            _codeText.text = fullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(typewriterSpeed * 0.5f);
        }

        float flickerTime = 0f;
        while (flickerTime < 0.35f)
        {
            flickerTime += Time.unscaledDeltaTime;
            float alpha = 0.65f + Mathf.Abs(Mathf.Sin(flickerTime * 20f)) * 0.35f;
            _codeText.color = new Color(CodeGold.r, CodeGold.g, CodeGold.b, alpha);
            yield return null;
        }

        _codeText.color = CodeGold;
    }

    private IEnumerator PlayPrometheusLine(string flag)
    {
        var state = GameManager.Instance?.State;
        var dialogueManager = FindObjectOfType<DialogueManager>();
        if (state == null || dialogueManager == null)
        {
            yield break;
        }

        state.SetFlag(flag);
        dialogueManager.TriggerDialogues("BigLevel2_WinBlack");

        while (dialogueManager.HasPending)
        {
            yield return null;
        }
    }

    private IEnumerator AnyClickLoop()
    {
        float blinkTimer = 0f;
        while (!ClickedToReturn())
        {
            blinkTimer += Time.unscaledDeltaTime;
            float alpha = 0.35f + Mathf.Abs(Mathf.Sin(blinkTimer * Mathf.PI / blinkInterval)) * 0.45f;
            _anyKeyText.color = new Color(PromptGray.r, PromptGray.g, PromptGray.b, alpha);
            yield return null;
        }

        Input.ResetInputAxes();
        _routineComplete = true;
        _controller?.ReturnToDesktop();
    }

    private bool ClickedToReturn()
    {
        return Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown;
    }
}
