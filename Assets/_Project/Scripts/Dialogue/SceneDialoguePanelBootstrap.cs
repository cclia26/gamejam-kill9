using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates the Prometheus floating portrait/dialogue panel in gameplay scenes.
/// Desktop has a hand-authored panel; other scenes get this runtime copy so
/// DialogueManager can use the same DialogueDisplay code everywhere.
/// </summary>
public static class SceneDialoguePanelBootstrap
{
    private static readonly HashSet<string> TargetScenes = new HashSet<string>
    {
        "Level1_Home",
        "Level1_Rooms",
        "Level2_Empathy",
        "Level3_Will"
    };

    private const string PortraitSpriteGuid = "50341bdce0a487a43a65edaaf56fd605";

    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (!TargetScenes.Contains(scene.name))
        {
            return;
        }

        DialogueDisplay existing = Object.FindObjectOfType<DialogueDisplay>(true);
        if (existing != null && existing.gameObject.name == "DialoguePanel" && existing.GetComponentInParent<Canvas>()?.gameObject.name == "RuntimeDialogueCanvas")
        {
            BringToTop(existing.gameObject);
            return;
        }

        EnsureEventSystem();
        EnsureDialogueRuntime();

        GameObject canvasGo = new GameObject("RuntimeDialogueCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(320f, 180f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = new GameObject("DialoguePanel", typeof(RectTransform));
        panelGo.SetActive(false);
        panelGo.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        RectTransform portrait = CreatePortrait(panelGo.transform);
        CanvasGroup portraitGroup = portrait.gameObject.AddComponent<CanvasGroup>();
        portraitGroup.alpha = 0f;
        portraitGroup.blocksRaycasts = true;

        RectTransform bubble = CreateBubble(panelGo.transform);
        CanvasGroup bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
        bubbleGroup.alpha = 0f;
        bubbleGroup.blocksRaycasts = false;

        TMP_Text content = CreateContentText(bubble);

        DialogueDisplay display = panelGo.AddComponent<DialogueDisplay>();
        SetPrivateField(display, "portraitRect", portrait);
        SetPrivateField(display, "portraitGroup", portraitGroup);
        SetPrivateField(display, "bubblePanel", bubble);
        SetPrivateField(display, "contentText", content);
        SetPrivateField(display, "bubbleGroup", bubbleGroup);
        SetPrivateField(display, "leftMargin", 8f);
        SetPrivateField(display, "rightMargin", 26f);
        SetPrivateField(display, "portraitY", 70f);
        SetPrivateField(display, "bubbleGap", 10f);
        SetPrivateField(display, "typeSpeed", 0.04f);
        SetPrivateField(display, "hideDelay", 1.5f);

        panelGo.SetActive(true);
        BringToTop(panelGo);
    }



    private static void EnsureDialogueRuntime()
    {
        GameObject runtimeGo;

        if (GameManager.Instance == null)
        {
            runtimeGo = new GameObject("RuntimeDialogueManagers");
            runtimeGo.AddComponent<GameManager>();
        }
        else
        {
            runtimeGo = GameManager.Instance.gameObject;
        }

        if (runtimeGo.GetComponent<DialogueManager>() == null && Object.FindObjectOfType<DialogueManager>() == null)
        {
            runtimeGo.AddComponent<DialogueManager>();
        }

        if (runtimeGo.GetComponent<EndingManager>() == null && Object.FindObjectOfType<EndingManager>() == null)
        {
            runtimeGo.AddComponent<EndingManager>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();
    }
    private static RectTransform CreatePortrait(Transform parent)
    {
        GameObject go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(12f, 12f);
        Image image = go.GetComponent<Image>();
        image.color = Color.white;
        image.sprite = LoadPortraitSprite();
        image.raycastTarget = true;
        return rt;
    }

    private static RectTransform CreateBubble(Transform parent)
    {
        GameObject go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(170f, 25f);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5529412f);
        image.raycastTarget = false;
        return rt;
    }

    private static TMP_Text CreateContentText(RectTransform parent)
    {
        GameObject go = new GameObject("ContentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 0f);
        rt.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("simhei SDF");
        if (font != null)
        {
            text.font = font;
        }
        text.fontSize = 8f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.text = string.Empty;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }


    private static Sprite LoadPortraitSprite()
    {
#if UNITY_EDITOR
        string path = AssetDatabase.GUIDToAssetPath(PortraitSpriteGuid);
        if (!string.IsNullOrEmpty(path))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif
        return null;
    }

    private static void BringToTop(GameObject go)
    {
        Canvas canvas = go.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
        }
        go.transform.SetAsLastSibling();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}


