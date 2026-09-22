using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static HashUIFactory;

/// <summary>Stack_Queue 씬의 고정 용량 Stack·Queue UI와 직렬화 참조를 생성한다.</summary>
public static class StackQueueSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Stack_Queue.unity";
    private const string PrefabFolder = "Assets/_Projects/Prefabs";
    private const float ContainerWidth = 1000f;
    private const float ContainerPadding = 16f;
    private const float SlotWidth = 112f;
    private const float SlotSpacing = 8f;
    private const int Capacity = (int)((ContainerWidth - ContainerPadding * 2f + SlotSpacing) / (SlotWidth + SlotSpacing));

    // Stack_Queue 씬을 열어 둔 상태에서 UI만 재생성하고 기존 카메라와 조명은 유지한다.
    [MenuItem("Structura/Build Stack Queue UI")]
    public static void Build()
    {
        return;
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play Mode before building Stack Queue UI.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open Stack_Queue scene before building its UI.");
            return;
        }

        System.IO.Directory.CreateDirectory(PrefabFolder);
        var slotPrefab = BuildSlotPrefab();
        var canvas = FindOrCreateCanvas();
        ClearCanvas(canvas.transform);
        ConfigureCanvas(canvas);
        EnsureEventSystem();

        var background = Box("StackQueueUI", canvas.transform, new Color(0.035f, 0.06f, 0.10f));
        Stretch(background.rectTransform);
        var controls = background.gameObject.AddComponent<StackQueueControls>();
        var view = background.gameObject.AddComponent<StackQueueUIView>();
        BuildHeader(background.transform, controls);
        BuildOperations(background.transform, controls);
        BuildContainers(background.transform, view, slotPrefab);
        BuildSetup(background.transform, controls);
        ConnectController(background.gameObject, controls, view);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    // 기존 Canvas가 있으면 재사용하고 없으면 UI 렌더링에 필요한 컴포넌트를 함께 만든다.
    private static Canvas FindOrCreateCanvas()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
            return canvas;

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        return canvasObject.GetComponent<Canvas>();
    }

    // 이 Builder가 소유하는 Canvas 내부 UI만 제거해 실행할 때마다 중복되지 않게 한다.
    private static void ClearCanvas(Transform canvas)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvas.GetChild(i).gameObject);
    }

    // 레이아웃 계산의 기준이 되는 1600x900 화면과 Overlay 렌더링을 설정한다.
    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    // 빈 씬에서도 InputField와 Button이 입력 이벤트를 받을 수 있게 EventSystem을 준비한다.
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    // 제목, 타입, 현재 용량과 조작 결과를 상단에 표시한다.
    private static void BuildHeader(Transform parent, StackQueueControls controls)
    {
        var title = Label("Title", parent, "STRUCTURA  /  STACK & QUEUE", 30);
        Place(title.rectTransform, 44f, 20f, 900f, 48f);

        var type = Label("Type", parent, "Choose a value type to begin.", 18);
        Place(type.rectTransform, 44f, 70f, 720f, 28f);
        Ref(controls, "_typeText", type);

        var stats = Label("Stats", parent, "Count: 0 / " + Capacity, 18);
        Place(stats.rectTransform, 1030f, 70f, 526f, 28f);
        stats.alignment = TextAnchor.MiddleRight;
        Ref(controls, "_statsText", stats);

        var feedback = Label("Feedback", parent, "Select a value type, then press Start.", 18);
        Place(feedback.rectTransform, 44f, 198f, 1512f, 28f);
        Ref(controls, "_feedbackText", feedback);
    }

    // Value 입력과 두 공통 조작 버튼을 한 패널에 배치한다.
    private static void BuildOperations(Transform parent, StackQueueControls controls)
    {
        var panel = Box("Operations", parent, Panel);
        Place(panel.rectTransform, 44f, 110f, 1512f, 76f);
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        Ref(controls, "_operations", group);

        var value = Input("ValueInput", panel.transform, "Value");
        Place((RectTransform)value.transform, 16f, 16f, 520f, 44f);
        Ref(controls, "_valueInput", value);

        var insert = Button("PushEnqueueButton", panel.transform, "Push / Enqueue", Accent);
        Place((RectTransform)insert.transform, 556f, 16f, 280f, 44f);
        Ref(controls, "_pushEnqueueButton", insert);

        var remove = Button("PopDequeueButton", panel.transform, "Pop / Dequeue", new Color(0.15f, 0.23f, 0.32f));
        Place((RectTransform)remove.transform, 856f, 16f, 280f, 44f);
        Ref(controls, "_popDequeueButton", remove);

        var hint = Label("Hint", panel.transform,
            "Each click changes both containers; only their removal endpoint differs.", 16);
        Place(hint.rectTransform, 1156f, 16f, 340f, 44f);
        hint.alignment = TextAnchor.MiddleCenter;
    }

    // 중앙의 같은 폭 컨테이너 두 개에 8개의 고정 슬롯을 만들고 각 끝 위치 텍스트를 연결한다.
    private static void BuildContainers(Transform parent, StackQueueUIView view, StackQueueSlotUIView slotPrefab)
    {
        var stackSlots = BuildContainer(parent, "Stack", "LIFO  /  last in, first out", 260f, 260f, slotPrefab);
        var queueSlots = BuildContainer(parent, "Queue", "FIFO  /  first in, first out", 260f, 540f, slotPrefab);
        SetSlots(view, "_stackSlots", stackSlots);
        SetSlots(view, "_queueSlots", queueSlots);

        var stackTop = Endpoint(parent, "StackTop", "TOP\nempty", 1280f, 302f);
        Ref(view, "_stackTopText", stackTop);

        var queueFront = Endpoint(parent, "QueueFront", "FRONT\nempty", 40f, 582f);
        Ref(view, "_queueFrontText", queueFront);

        var queueRear = Endpoint(parent, "QueueRear", "REAR\nempty", 1280f, 582f);
        Ref(view, "_queueRearText", queueRear);

        var stackArrow = Label("StackArrow", parent, "← top", 16);
        Place(stackArrow.rectTransform, 1210f, 440f, 160f, 28f);
        stackArrow.alignment = TextAnchor.MiddleCenter;

        var queueArrow = Label("QueueArrow", parent, "front ←                              rear →", 16);
        Place(queueArrow.rectTransform, 260f, 718f, ContainerWidth, 28f);
        queueArrow.alignment = TextAnchor.MiddleCenter;
    }

    // 컨테이너 폭과 슬롯 폭·간격으로 Capacity가 정확히 8이 되도록 가로 레이아웃을 만든다.
    private static StackQueueSlotUIView[] BuildContainer(Transform parent, string name, string subtitle,
        float x, float y, StackQueueSlotUIView slotPrefab)
    {
        var label = Label(name + "Label", parent, name.ToUpperInvariant(), 25);
        Place(label.rectTransform, 44f, y + 40f, 190f, 34f);

        var description = Label(name + "Description", parent, subtitle, 16);
        Place(description.rectTransform, 44f, y + 78f, 190f, 46f);
        description.alignment = TextAnchor.UpperLeft;

        var container = Box(name + "Container", parent, Panel);
        Place(container.rectTransform, x, y, ContainerWidth, 164f);
        var slotRoot = Rect("SlotRoot", container.transform);
        Stretch(slotRoot, ContainerPadding, 36f, ContainerPadding, 16f);
        var row = Row(slotRoot.gameObject, SlotSpacing);
        row.childAlignment = TextAnchor.MiddleLeft;

        var capacityLabel = Label("Capacity", container.transform, "capacity  " + Capacity, 14);
        Place(capacityLabel.rectTransform, 16f, 6f, 200f, 24f);

        var slots = new StackQueueSlotUIView[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            var slot = PrefabUtility.InstantiatePrefab(slotPrefab, slotRoot) as StackQueueSlotUIView;
            slot.name = name + " Slot " + i;
            slots[i] = slot;
        }

        return slots;
    }

    // 컨테이너 끝의 TOP, FRONT, REAR 위치를 작은 독립 카드로 표시한다.
    private static Text Endpoint(Transform parent, string name, string label, float x, float y)
    {
        var card = Box(name, parent, new Color(0.15f, 0.23f, 0.32f));
        Place(card.rectTransform, x, y, 180f, 80f);
        var text = Label("Text", card.transform, label, 16);
        Stretch(text.rectTransform, 8f, 6f, 8f, 6f);
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 16;
        return text;
    }

    // 한 슬롯 프리팹에 번호, 값, 자료구조 순서 문구를 연결한다.
    private static StackQueueSlotUIView BuildSlotPrefab()
    {
        var root = Box("StackQueueSlotUI", null, new Color(0.065f, 0.10f, 0.15f));
        Place(root.rectTransform, 0f, 0f, SlotWidth, 112f);
        Fixed(root.gameObject, SlotWidth, 112f);
        root.gameObject.AddComponent<CanvasGroup>();
        var view = root.gameObject.AddComponent<StackQueueSlotUIView>();

        var index = Label("IndexText", root.transform, "[0]", 14);
        Place(index.rectTransform, 8f, 8f, SlotWidth - 16f, 22f);
        index.alignment = TextAnchor.MiddleCenter;

        var value = Label("ValueText", root.transform, "empty", 19);
        Place(value.rectTransform, 8f, 35f, SlotWidth - 16f, 38f);
        value.alignment = TextAnchor.MiddleCenter;
        value.resizeTextForBestFit = true;
        value.resizeTextMinSize = 11;
        value.resizeTextMaxSize = 19;

        var order = Label("OrderText", root.transform, string.Empty, 12);
        Place(order.rectTransform, 8f, 78f, SlotWidth - 16f, 22f);
        order.alignment = TextAnchor.MiddleCenter;
        Ref(view, "_indexText", index);
        Ref(view, "_valueText", value);
        Ref(view, "_orderText", order);
        Ref(view, "_background", root);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/StackQueueSlotUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<StackQueueSlotUIView>();
    }

    // 시작 전에는 기존 자료구조 씬과 같은 타입 선택 패널을 띄운다.
    private static void BuildSetup(Transform parent, StackQueueControls controls)
    {
        var overlay = Box("TypeSelectionPanel", parent, new Color(0.01f, 0.02f, 0.04f, 0.94f));
        Stretch(overlay.rectTransform);
        var card = Box("Card", overlay.transform, Panel);
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        card.rectTransform.sizeDelta = new Vector2(680f, 360f);

        var title = Label("Title", card.transform, "Create Stack & Queue", 32);
        Place(title.rectTransform, 44f, 24f, 592f, 52f);
        var subtitle = Label("Subtitle", card.transform, "Choose one value type for this session.", 19);
        Place(subtitle.rectTransform, 44f, 88f, 592f, 32f);
        var typeLabel = Label("ValueTypeLabel", card.transform, "VALUE TYPE", 16);
        Place(typeLabel.rectTransform, 44f, 140f, 592f, 28f);
        var type = Types("ValueTypeDropdown", card.transform);
        Place((RectTransform)type.transform, 44f, 180f, 592f, 48f);
        var start = Button("StartButton", card.transform, "Start", Accent);
        Place((RectTransform)start.transform, 44f, 268f, 592f, 52f);
        Ref(controls, "_setupPanel", overlay.gameObject);
        Ref(controls, "_valueTypeDropdown", type);
        Ref(controls, "_startButton", start);
    }

    // UI 참조와 Controller를 연결해 생성 직후에도 버튼이 동작하게 한다.
    private static void ConnectController(GameObject root, StackQueueControls controls, StackQueueUIView view)
    {
        var controller = Object.FindFirstObjectByType<StackQueueController>();
        if (controller == null)
            controller = new GameObject("StackQueueController").AddComponent<StackQueueController>();

        Ref(controller, "_controls", controls);
        Ref(controller, "_view", view);
        EditorUtility.SetDirty(controller);
    }

    // View의 직렬화 배열에 Builder가 생성한 각 슬롯을 순서대로 저장한다.
    private static void SetSlots(StackQueueUIView view, string fieldName, StackQueueSlotUIView[] slots)
    {
        var serialized = new SerializedObject(view);
        var property = serialized.FindProperty(fieldName);
        property.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
