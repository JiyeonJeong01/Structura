using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static HashUIFactory;

/// <summary>메뉴에서 실행하면 기존 씬 폴더에 LobyScene을 생성하고 이동 대상을 등록한다.</summary>
public static class LobySceneBuilder
{
    private const string ScenePath = "Assets/Scenes/LobyScene.unity";
    private static readonly string[] SceneNames = { "HashTableScene", "DequeScene", "Stack_Queue" };
    private static readonly string[] Captions = { "HashTable", "Deque", "Stack/Queue" };

    [MenuItem("Structura/Build Loby Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play Mode before building LobyScene.");
            return;
        }

        // 이미지 삽입이나 수동 배치를 마친 기존 로비를 재실행으로 덮어쓰지 않는다.
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning($"{ScenePath} already exists. Open it to edit the lobby.");
            return;
        }

        foreach (string name in SceneNames)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>($"Assets/Scenes/{name}.unity") == null)
            {
                Debug.LogError($"Target scene not found: {name}");
                return;
            }
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = new Color(0.035f, 0.06f, 0.10f);

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var background = Box("LobyUI", canvasObject.transform, new Color(0.035f, 0.06f, 0.10f));
        Stretch(background.rectTransform);
        var content = Rect("Content", background.transform);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1440f, 760f);

        var title = Text("Title", content, "STRUCTURA", 46);
        Place(title.rectTransform, 0f, 16f, 600f, 64f);
        var subtitle = Text("Subtitle", content, "Choose a data structure", 22);
        Place(subtitle.rectTransform, 0f, 88f, 650f, 38f);

        var previewRoot = Rect("Previews", content);
        Place(previewRoot, 500f, 208f, 880f, 495f);
        for (int i = 0; i < SceneNames.Length; i++)
            BuildEntry(content, previewRoot, i);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Could not save LobyScene.");
            return;
        }

        RegisterScenes();
        Selection.activeGameObject = background.gameObject;
        Debug.Log($"Created {ScenePath}. Assign each PreviewImage's Source Image under Canvas/LobyUI/Content/Previews.");
    }

    private static void BuildEntry(Transform parent, Transform previewRoot, int index)
    {
        var hitArea = Box(SceneNames[index] + "Button", parent, Color.clear);
        Place(hitArea.rectTransform, 0f, 248f + index * 144f, 380f, 108f);
        var button = hitArea.gameObject.AddComponent<UnityEngine.UI.Button>();
        var visual = Box("Visual", hitArea.transform, Panel);
        Stretch(visual.rectTransform);
        visual.raycastTarget = false;
        button.targetGraphic = visual;
        var caption = Text("Caption", visual.transform, Captions[index], 30);
        Stretch(caption.rectTransform, 28f, 0f, 24f, 0f);

        var preview = Box(SceneNames[index] + "Preview", previewRoot, Panel);
        Stretch(preview.rectTransform);
        preview.raycastTarget = false;
        var group = preview.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        var placeholder = Text("Placeholder", preview.transform, Captions[index] + "\nSCENE PREVIEW", 28);
        Stretch(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.Center;
        // 이미지를 나중에 지정하면 안내 문구 위에 표시된다. 원본 비율은 유지한다.
        var image = Box("PreviewImage", preview.transform, Color.white);
        Stretch(image.rectTransform);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;

        var effect = hitArea.gameObject.AddComponent<LobySceneButton>();
        Ref(effect, "_visual", visual.rectTransform);
        Ref(effect, "_preview", preview.rectTransform);
        Ref(effect, "_previewGroup", group);
        Ref(effect, "_previewImage", image);
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("_sceneName").stringValue = SceneNames[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value, float size)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private static void RegisterScenes()
    {
        // 기존 씬 순서는 보존하고 누락된 씬만 추가하며 기존 비활성 대상은 활성화한다.
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var paths = new List<string> { ScenePath };
        foreach (string name in SceneNames)
            paths.Add($"Assets/Scenes/{name}.unity");
        foreach (string path in paths)
        {
            int index = scenes.FindIndex(entry => entry.path == path);
            if (index >= 0)
                scenes[index] = new EditorBuildSettingsScene(path, true);
            else
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
