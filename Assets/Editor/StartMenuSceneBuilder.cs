using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StartMenuSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/StartMenu.unity";
    private const string DesktopScenePath = "Assets/_Project/Scenes/Desktop.unity";

    [MenuItem("KILL-9/Build Start Menu Scene")]
    public static void BuildStartMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "StartMenu";

        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.012f, 0.01f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var controllerObject = new GameObject("StartMenuController");
        controllerObject.AddComponent<StartMenuController>();

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateTitle(canvasObject.transform);
        CreateMenuButton(canvasObject.transform, controllerObject.GetComponent<StartMenuController>(), "Button_StartGame", "开始游戏", new Vector2(180f, 210f), true);
        CreateMenuButton(canvasObject.transform, controllerObject.GetComponent<StartMenuController>(), "Button_QuitGame", "退出游戏", new Vector2(180f, 120f), false);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("StartMenu scene generated and added to Build Settings: " + ScenePath);
    }

    private static void CreateTitle(Transform parent)
    {
        var titleObject = new GameObject("Title_Kill9");
        titleObject.transform.SetParent(parent, false);

        var rect = titleObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(92f, -72f);
        rect.sizeDelta = new Vector2(720f, 160f);

        var text = titleObject.AddComponent<Text>();
        text.text = "KILL -9";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 96;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color(1f, 0.72f, 0.18f, 1f);
        text.raycastTarget = false;
    }

    private static void CreateMenuButton(Transform parent, StartMenuController controller, string objectName, string label, Vector2 anchoredPosition, bool startButton)
    {
        var buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 60f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.065f, 0.045f, 0.92f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.065f, 0.045f, 0.92f);
        colors.highlightedColor = new Color(0.24f, 0.18f, 0.09f, 1f);
        colors.pressedColor = new Color(0.45f, 0.31f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        button.colors = colors;

        if (startButton)
            button.onClick.AddListener(controller.StartGame);
        else
            button.onClick.AddListener(controller.QuitGame);

        var labelObject = new GameObject("Text");
        labelObject.transform.SetParent(buttonObject.transform, false);

        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var text = labelObject.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.82f, 0.38f, 1f);
        text.raycastTarget = false;
    }

    private static void AddSceneToBuildSettings()
    {
        var startScene = new EditorBuildSettingsScene(ScenePath, true);
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.path != ScenePath)
            .ToList();

        scenes.Insert(0, startScene);

        if (!scenes.Any(s => s.path == DesktopScenePath))
            scenes.Add(new EditorBuildSettingsScene(DesktopScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
