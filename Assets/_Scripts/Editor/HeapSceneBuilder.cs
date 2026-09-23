using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static HashUIFactory;

/// <summary>Heap 전용 씬 UI와 프리팹을 생성하고 Controller/View/Controls 참조를 연결한다.</summary>
public static class HeapSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/HeapScene.unity";
    private const string PrefabFolder = "Assets/_Projects/Prefabs";
    private const int Capacity = 15;

    [MenuItem("Structura/Build Heap UI")]
    public static void Build()
    {
        return;

        if (Application.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");

        System.IO.Directory.CreateDirectory(PrefabFolder);
        var scene = OpenOrCreateScene();
        var nodePrefab = BuildNodePrefab();
        var slotPrefab = BuildArraySlotPrefab();
        var canvas = FindOrCreateCanvas();
        ClearCanvas(canvas.transform);
        ConfigureCanvas(canvas);
        EnsureEventSystem();

        var background = Box("HeapUI", canvas.transform, new Color(0.035f, 0.06f, 0.10f));
        Stretch(background.rectTransform);
        var controls = background.gameObject.AddComponent<HeapControls>();
        var view = background.gameObject.AddComponent<HeapUIView>();

        BuildHeader(background.transform, controls);
        BuildOperations(background.transform, controls);
        BuildHeapPanel(background.transform, view, nodePrefab, slotPrefab);
        BuildSetup(background.transform, controls);
        ConnectController(controls, view);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Scene OpenOrCreateScene()
    {
        if (EditorSceneManager.GetActiveScene().path == ScenePath)
            return EditorSceneManager.GetActiveScene();

        var scene = System.IO.File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (scene.path != ScenePath)
            EditorSceneManager.SaveScene(scene, ScenePath);

        return scene;
    }

    private static Canvas FindOrCreateCanvas()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
            return canvas;

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        return canvasObject.GetComponent<Canvas>();
    }

    private static void ClearCanvas(Transform canvas)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvas.GetChild(i).gameObject);
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static void BuildHeader(Transform parent, HeapControls controls)
    {
        var title = TmpLabel("Title", parent, "STRUCTURA  /  HEAP", 30);
        Place(title.rectTransform, 44f, 20f, 900f, 48f);

        var type = TmpLabel("Type", parent, "Choose a value type to begin.", 18);
        Place(type.rectTransform, 44f, 70f, 720f, 28f);
        Ref(controls, "_typeText", type);

        var stats = TmpLabel("Stats", parent, "Count: 0 / 15", 18);
        Place(stats.rectTransform, 1030f, 70f, 526f, 28f);
        stats.alignment = TextAlignmentOptions.Right;
        Ref(controls, "_statsText", stats);

        var feedback = TmpLabel("Feedback", parent, "Select a value type, then press Start.", 18);
        Place(feedback.rectTransform, 44f, 198f, 1512f, 28f);
        Ref(controls, "_feedbackText", feedback);
    }

    private static void BuildOperations(Transform parent, HeapControls controls)
    {
        var panel = Box("Operations", parent, Panel);
        Place(panel.rectTransform, 44f, 110f, 1512f, 76f);
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        Ref(controls, "_operations", group);

        var value = TmpInput("ValueInput", panel.transform, "Value");
        Place((RectTransform)value.transform, 16f, 16f, 520f, 44f);
        Ref(controls, "_valueInput", value);

        string[] names = { "Push", "Pop", "Step", "Play" };
        string[] fields = { "_pushButton", "_popButton", "_stepButton", "_playButton" };
        for (int i = 0; i < names.Length; i++)
        {
            var color = i == 0 ? Accent : new Color(0.15f, 0.23f, 0.32f);
            var button = TmpButton(names[i] + "Button", panel.transform, names[i], color);
            Place((RectTransform)button.transform, 556f + i * 156f, 16f, 140f, 44f);
            Ref(controls, fields[i], button);
        }

        var hint = TmpLabel("Hint", panel.transform, "Push/Pop starts one operation. Step and Play perform heap swaps.", 16);
        Place(hint.rectTransform, 1196f, 16f, 300f, 44f);
        hint.alignment = TextAlignmentOptions.Center;
    }

    private static void BuildHeapPanel(Transform parent, HeapUIView view, HeapNodeUIView nodePrefab, HeapArraySlotUIView slotPrefab)
    {
        var treePanel = Box("TreePanel", parent, new Color(0.055f, 0.085f, 0.13f));
        Place(treePanel.rectTransform, 44f, 240f, 1512f, 430f);
        var treeTitle = TmpLabel("TreeTitle", treePanel.transform, "TREE  /  fixed heap indices", 18);
        Place(treeTitle.rectTransform, 18f, 8f, 500f, 28f);

        var effectRoot = Rect("EffectRoot", parent);
        Stretch(effectRoot);
        effectRoot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Ref(view, "_effectRoot", effectRoot);

        Vector2[] centers =
        {
            new Vector2(756f, -72f),
            new Vector2(448f, -168f), new Vector2(1064f, -168f),
            new Vector2(292f, -270f), new Vector2(604f, -270f), new Vector2(908f, -270f), new Vector2(1220f, -270f),
            new Vector2(172f, -368f), new Vector2(324f, -368f), new Vector2(520f, -368f), new Vector2(672f, -368f),
            new Vector2(824f, -368f), new Vector2(976f, -368f), new Vector2(1136f, -368f), new Vector2(1288f, -368f)
        };

        var links = new Image[Capacity - 1];
        for (int child = 1; child < Capacity; child++)
            links[child - 1] = BuildLink(treePanel.transform, centers[(child - 1) / 2], centers[child]);
        SetArray(view, "_links", links);

        var nodes = new HeapNodeUIView[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var node = PrefabUtility.InstantiatePrefab(nodePrefab, treePanel.transform) as HeapNodeUIView;
            node.name = "Heap Node " + i;
            var rect = node.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centers[i];
            nodes[i] = node;
        }
        SetArray(view, "_nodes", nodes);

        var arrayPanel = Box("ArrayPanel", parent, new Color(0.055f, 0.085f, 0.13f));
        Place(arrayPanel.rectTransform, 44f, 690f, 1512f, 148f);
        var arrayTitle = TmpLabel("ArrayTitle", arrayPanel.transform, "ARRAY  /  internal heap order", 18);
        Place(arrayTitle.rectTransform, 18f, 8f, 500f, 28f);
        var slotRoot = Rect("SlotRoot", arrayPanel.transform);
        Place(slotRoot, 18f, 44f, 1476f, 86f);
        var row = slotRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 7f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;

        var slots = new HeapArraySlotUIView[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var slot = PrefabUtility.InstantiatePrefab(slotPrefab, slotRoot) as HeapArraySlotUIView;
            slot.name = "Heap Array Slot " + i;
            slots[i] = slot;
        }
        SetArray(view, "_arraySlots", slots);
    }

    private static Image BuildLink(Transform parent, Vector2 from, Vector2 to)
    {
        var line = Box("Link", parent, new Color(0.08f, 0.13f, 0.18f, 0.45f));
        var rect = line.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = from;
        Vector2 direction = to - from;
        rect.sizeDelta = new Vector2(direction.magnitude, 4f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        return line;
    }

    private static HeapNodeUIView BuildNodePrefab()
    {
        var root = Box("HeapNodeUI", null, new Color(0.045f, 0.065f, 0.095f));
        Place(root.rectTransform, 0f, 0f, 92f, 72f);
        Fixed(root.gameObject, 92f, 72f);
        root.gameObject.AddComponent<CanvasGroup>();
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);
        outline.enabled = false;
        var view = root.gameObject.AddComponent<HeapNodeUIView>();
        BuildCardTexts(root.transform, view, "_indexText", "_valueText", "_stateText", 92f, 72f);
        Ref(view, "_background", root);
        Ref(view, "_outline", outline);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/HeapNodeUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<HeapNodeUIView>();
    }

    private static HeapArraySlotUIView BuildArraySlotPrefab()
    {
        var root = Box("HeapArraySlotUI", null, new Color(0.045f, 0.065f, 0.095f));
        Place(root.rectTransform, 0f, 0f, 92f, 84f);
        Fixed(root.gameObject, 92f, 84f);
        root.gameObject.AddComponent<CanvasGroup>();
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);
        outline.enabled = false;
        var view = root.gameObject.AddComponent<HeapArraySlotUIView>();
        BuildCardTexts(root.transform, view, "_indexText", "_valueText", "_stateText", 92f, 84f);
        Ref(view, "_background", root);
        Ref(view, "_outline", outline);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/HeapArraySlotUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<HeapArraySlotUIView>();
    }

    private static void BuildCardTexts(Transform parent, Object view, string indexField, string valueField, string stateField,
        float width, float height)
    {
        var index = TmpLabel("IndexText", parent, "[0]", 13);
        Place(index.rectTransform, 6f, 4f, width - 12f, 18f);
        index.alignment = TextAlignmentOptions.Center;

        var clip = Rect("ValueClip", parent);
        Place(clip, 6f, 24f, width - 12f, 30f);
        clip.gameObject.AddComponent<RectMask2D>();
        var value = TmpLabel("ValueText", clip, "empty", 18);
        Stretch(value.rectTransform);
        value.alignment = TextAlignmentOptions.Center;
        value.enableWordWrapping = false;
        value.overflowMode = TextOverflowModes.Ellipsis;

        var state = TmpLabel("StateText", parent, string.Empty, 10);
        Place(state.rectTransform, 6f, height - 18f, width - 12f, 14f);
        state.alignment = TextAlignmentOptions.Center;

        Ref(view, indexField, index);
        Ref(view, valueField, value);
        Ref(view, stateField, state);
    }

    private static void BuildSetup(Transform parent, HeapControls controls)
    {
        var overlay = Box("TypeSelectionPanel", parent, new Color(0.01f, 0.02f, 0.04f, 0.94f));
        Stretch(overlay.rectTransform);
        var card = Box("Card", overlay.transform, Panel);
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        card.rectTransform.sizeDelta = new Vector2(680f, 360f);

        var title = TmpLabel("Title", card.transform, "Create a heap", 32);
        Place(title.rectTransform, 44f, 24f, 592f, 52f);
        var subtitle = TmpLabel("Subtitle", card.transform, "Choose one value type for this session.", 19);
        Place(subtitle.rectTransform, 44f, 88f, 592f, 32f);
        var typeLabel = TmpLabel("ValueTypeLabel", card.transform, "VALUE TYPE", 16);
        Place(typeLabel.rectTransform, 44f, 140f, 592f, 28f);
        var type = TmpDropdown("ValueTypeDropdown", card.transform);
        Place((RectTransform)type.transform, 44f, 180f, 592f, 48f);
        var start = TmpButton("StartButton", card.transform, "Start", Accent);
        Place((RectTransform)start.transform, 44f, 268f, 592f, 52f);
        Ref(controls, "_setupPanel", overlay.gameObject);
        Ref(controls, "_valueTypeDropdown", type);
        Ref(controls, "_startButton", start);
    }

    private static void ConnectController(HeapControls controls, HeapUIView view)
    {
        var controller = Object.FindFirstObjectByType<HeapController>();
        if (controller == null)
            controller = new GameObject("HeapController").AddComponent<HeapController>();

        Ref(controller, "_controls", controls);
        Ref(controller, "_heapView", view);
        EditorUtility.SetDirty(controller);
    }

    private static TMP_Text TmpLabel(string name, Transform parent, string value, int size)
    {
        var rect = Rect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static Button TmpButton(string name, Transform parent, string caption, Color color)
    {
        var image = Box(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        var text = TmpLabel("Text", image.transform, caption, 20);
        Stretch(text.rectTransform, 8f, 0f, 8f, 0f);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TMP_InputField TmpInput(string name, Transform parent, string placeholder)
    {
        var image = Box(name, parent, Color.white);
        image.color = new Color(0.90f, 0.94f, 0.96f);
        var input = image.gameObject.AddComponent<TMP_InputField>();
        var viewport = Rect("Text Area", image.transform);
        Stretch(viewport, 10f, 4f, 10f, 4f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var placeholderText = TmpLabel("Placeholder", viewport, placeholder, 18);
        Stretch(placeholderText.rectTransform);
        placeholderText.color = new Color(0.35f, 0.42f, 0.48f);
        var text = TmpLabel("Text", viewport, string.Empty, 18);
        Stretch(text.rectTransform);
        text.color = Panel;
        text.raycastTarget = true;
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.targetGraphic = image;
        return input;
    }

    private static TMP_Dropdown TmpDropdown(string name, Transform parent)
    {
        var image = Box(name, parent, Color.white);
        image.color = new Color(0.90f, 0.94f, 0.96f);
        var dropdown = image.gameObject.AddComponent<TMP_Dropdown>();

        var label = TmpLabel("Label", image.transform, "int", 19);
        Stretch(label.rectTransform, 12f, 0f, 46f, 0f);
        label.color = Panel;
        var arrow = TmpLabel("Arrow", image.transform, "v", 18);
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        arrow.rectTransform.anchoredPosition = new Vector2(-22f, 0f);
        arrow.rectTransform.sizeDelta = new Vector2(32f, 32f);
        arrow.color = Panel;
        arrow.alignment = TextAlignmentOptions.Center;

        var template = BuildDropdownTemplate(image.transform);
        dropdown.template = template;
        dropdown.captionText = label;
        dropdown.itemText = template.GetComponentInChildren<Toggle>(true).GetComponentInChildren<TMP_Text>(true);
        dropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("int"),
            new TMP_Dropdown.OptionData("float"),
            new TMP_Dropdown.OptionData("string")
        };
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static RectTransform BuildDropdownTemplate(Transform parent)
    {
        var templateImage = Box("Template", parent, Color.white);
        var template = templateImage.rectTransform;
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -2f);
        template.sizeDelta = new Vector2(0f, 132f);
        templateImage.color = new Color(0.90f, 0.94f, 0.96f);
        template.gameObject.SetActive(false);

        var scroll = template.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        var viewport = Box("Viewport", template, Color.white);
        Stretch(viewport.rectTransform);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("Content", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 132f);
        var item = Box("Item", content, Color.white);
        item.color = new Color(0.90f, 0.94f, 0.96f);
        item.rectTransform.anchorMin = new Vector2(0f, 1f);
        item.rectTransform.anchorMax = new Vector2(1f, 1f);
        item.rectTransform.pivot = new Vector2(0.5f, 1f);
        item.rectTransform.anchoredPosition = Vector2.zero;
        item.rectTransform.sizeDelta = new Vector2(0f, 40f);
        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = item;
        var checkmark = Box("Item Checkmark", item.transform, Accent);
        Place(checkmark.rectTransform, 8f, 8f, 24f, 24f);
        toggle.graphic = checkmark;
        var label = TmpLabel("Item Label", item.transform, "Option", 18);
        Stretch(label.rectTransform, 42f, 0f, 8f, 0f);
        label.color = Panel;
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        return template;
    }

    private static void SetArray<T>(Object target, string fieldName, T[] values) where T : Object
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(fieldName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
