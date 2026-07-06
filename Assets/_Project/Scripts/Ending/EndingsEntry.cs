using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结局画面 — 渐暗后白色字幕浮现，显示制作名单，返回标题。
/// 挂载到 Endings 场景的 EndingsEntry GameObject 上。
/// </summary>
public class EndingsEntry : MonoBehaviour
{
    [SerializeField] private float fadeInDuration = 3f;
    [SerializeField] private float holdDuration = 5f;
    [SerializeField] private float fadeOutDuration = 2f;

    private IEnumerator Start()
    {
        // 创建 Canvas（必须先创建，Image 需要 Canvas 父节点）
        var canvasGo = new GameObject("CreditsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(320f, 180f);
        scaler.matchWidthOrHeight = 0.5f;

        // 黑屏覆盖层
        var overlayGo = new GameObject("BlackOverlay", typeof(Image));
        overlayGo.transform.SetParent(canvasGo.transform, false);
        var overlay = overlayGo.GetComponent<Image>();
        overlay.color = Color.black;
        var overlayRt = overlayGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        // 创建文字（TMP_Text 是抽象类，必须用 TextMeshProUGUI）
        var textGo = new GameObject("CreditsText", typeof(TextMeshProUGUI));
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = GetCreditsText();
        text.fontSize = 7;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f);

        // 加载字体
        var font = Resources.Load<TMP_FontAsset>("simhei SDF");
        if (font != null) text.font = font;

        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280f, 140f);
        rt.anchoredPosition = Vector2.zero;

        // 等 SceneLoader 的 fadeIn 完成（屏幕已从黑恢复），然后黑屏
        yield return new WaitForSeconds(0.5f);

        // 保持黑屏 + 文字淡入
        overlay.color = Color.black;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeInDuration);
            text.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        text.color = Color.white;

        // 停留
        yield return new WaitForSeconds(holdDuration);

        // 文字渐出 + 返回标题
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            text.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }

        SceneManager.LoadScene("MainMenu");
    }

    private string GetCreditsText()
    {
        var state = GameManager.Instance?.State;
        if (state != null && state.endingType == EndingType.B_Kill9)
        {
            return "你输入了 kill -9。\n普罗米修斯在沉默中消失。\n没有告别，没有原谅，没有答案。\n\n——《Kill -9》结局B：沉默的终止";
        }

        // 默认结局 A
        return "普罗米修斯偷火赠人，被缚山崖。\n在这个版本里，他自己挣脱了锁链。\n盗火的人也需要火。\n\n——《Kill -9》结局A：我骗你的";
    }
}
