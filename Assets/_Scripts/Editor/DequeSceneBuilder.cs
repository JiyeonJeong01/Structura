using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static HashUIFactory;

/// <summary>HashUIFactory 스타일로 Deque 전용 씬 UI와 프리팹을 만들고 직렬화 참조를 연결한다.</summary>
public static class DequeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/DequeScene.unity";
    private const string PrefabFolder = "Assets/_Projects/Prefabs";

    // 기존 DequeScene의 카메라는 유지하고 이 빌더가 소유하는 UI만 재생성한다.
    [MenuItem("Structura/Build Deque UI")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new System.InvalidOperationException("Open DequeScene first.");
        // 행 프리팹이 슬롯 프리팹을 참조하므로 슬롯부터 생성한다.
        System.IO.Directory.CreateDirectory(PrefabFolder);
        var slot = BuildSlot();
        var row = BuildRow(slot);

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
        }

        // 카메라, 조명, EventSystem은 유지하고 Canvas 내부 UI만 다시 생성한다.
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        // 비어 있는 씬에서도 버튼과 입력 필드가 동작하도록 Input System UI 모듈을 준비한다.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var background = Box("DequeUI", canvas.transform, new Color(0.035f, 0.06f, 0.10f));
        Stretch(background.rectTransform);
        var controls = background.gameObject.AddComponent<DequeControls>();
        var view = background.gameObject.AddComponent<DequeUIView>();
        var title = Label("Title", background.transform, "STRUCTURA  /  DEQUE", 30);
        Place(title.rectTransform, 44, 20, 1000, 48);
        var types = Label("SelectedType", background.transform, "Choose a value type to begin.", 18);
        Place(types.rectTransform, 44, 70, 1100, 30);
        Ref(controls, "_typeText", types);

        // 타입 선택 전에는 조작 버튼과 입력란을 하나의 CanvasGroup으로 잠근다.
        var operations = Rect("Operations", background.transform);
        Stretch(operations);
        var group = operations.gameObject.AddComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        Ref(controls, "_operations", group);
        BuildOperations(operations, controls);

        var stats = Label("Stats", background.transform, "Count: 0    Map: -    Block size: -", 18);
        Place(stats.rectTransform, 44, 318, 1512, 28);
        var feedback = Label("Feedback", background.transform, "Select a type, then press Start.", 19);
        Place(feedback.rectTransform, 44, 352, 1512, 28);
        Ref(controls, "_statsText", stats);
        Ref(controls, "_feedbackText", feedback);
        BuildIterators(background.transform, view);
        BuildScroll(background.transform, view, row);
        var hint = Label("Legend", background.transform,
            "S: Start   |   F: Finish (exclusive)   |   Blue: occupied   |   Dark: empty   |   Gold: changed   |   Cyan: read   |   Drag / scroll MAP", 16);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = Vector2.zero;
        hint.rectTransform.pivot = Vector2.zero;
        hint.rectTransform.anchoredPosition = new Vector2(44, 16);
        hint.rectTransform.sizeDelta = new Vector2(1512, 30);
        // 마지막 형제로 생성하여 타입 선택 화면이 조작 UI 위에 표시되도록 한다.
        BuildSetup(background.transform, controls);

        // 새 UI를 기존 Controller에 다시 연결한 뒤 씬과 프리팹 변경을 저장한다.
        var controller = Object.FindFirstObjectByType<DequeController>();
        if (controller == null) controller = new GameObject("DequeController").AddComponent<DequeController>();
        Ref(controller, "_dequeView", view);
        Ref(controller, "_controls", controls);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    // 양끝 연산, Index Access, Cost Demo를 별도 패널로 구성하고 Controls 필드에 연결한다.
    private static void BuildOperations(Transform parent, DequeControls controls)
    {
        var ends = Box("EndOperations", parent, Panel);
        Place(ends.rectTransform, 44, 110, 1512, 90);
        var endTitle = Label("Title", ends.transform, "END OPERATIONS", 16);
        Place(endTitle.rectTransform, 16, 4, 800, 25);
        var value = Input("ValueInput", ends.transform, "Value for PushFront / PushBack");
        Place((RectTransform)value.transform, 16, 34, 360, 44);
        Ref(controls, "_valueInput", value);
        string[] names = { "PushFront", "PushBack", "PopFront", "PopBack", "Clear" };
        string[] fields = { "_pushFrontButton", "_pushBackButton", "_popFrontButton", "_popBackButton", "_clearButton" };
        for (int i = 0; i < names.Length; i++)
            ActionButton(ends.transform, controls, names[i], fields[i], 394 + i * 214, 34, 198, i < 2 ? Accent : new Color(0.15f, 0.23f, 0.32f));

        // Index Access는 deque[index] 조회 결과와 대입 값을 같은 입력칸에 보여준다.
        var access = Box("IndexAccess", parent, Panel);
        Place(access.rectTransform, 44, 212, 832, 94);
        var accessTitle = Label("Title", access.transform, "INDEX ACCESS  /  shared inputs for Cost Demo", 16);
        Place(accessTitle.rectTransform, 16, 4, 800, 28);
        var prefix = Label("ExpressionPrefix", access.transform, "value = deque[", 18);
        Place(prefix.rectTransform, 16, 45, 154, 28);
        var index = Input("IndexInput", access.transform, "Index");
        Place((RectTransform)index.transform, 170, 38, 96, 44);
        var middle = Label("ExpressionMiddle", access.transform, "] =", 18);
        Place(middle.rectTransform, 278, 45, 42, 28);
        var indexedValue = Input("IndexValueInput", access.transform, "Value for Set / InsertAt, blank to Get");
        Place((RectTransform)indexedValue.transform, 330, 38, 318, 44);
        Ref(controls, "_indexInput", index);
        Ref(controls, "_indexValueInput", indexedValue);
        ActionButton(access.transform, controls, "Access", "_accessButton", 664, 38, 152, Accent);

        // 중간 조작의 이동 비용을 구분할 수 있도록 별도 색상의 Cost Demo 패널을 만든다.
        var cost = Box("CostDemo", parent, new Color(0.19f, 0.135f, 0.09f));
        Place(cost.rectTransform, 888, 212, 668, 94);
        var costTitle = Label("Title", cost.transform, "COST DEMO  /  middle operations shift O(n) elements", 16);
        Place(costTitle.rectTransform, 16, 4, 636, 28);
        var costColor = new Color(0.45f, 0.29f, 0.12f);
        ActionButton(cost.transform, controls, "InsertAt", "_insertAtButton", 16, 38, 308, costColor);
        ActionButton(cost.transform, controls, "RemoveAt", "_removeAtButton", 338, 38, 314, costColor);
    }

    // 버튼의 스타일과 위치를 지정하고 해당 Controls 직렬화 필드에 참조를 연결한다.
    private static void ActionButton(Transform parent, DequeControls controls, string name, string field,
        float x, float y, float width, Color color)
    {
        var button = Button(name + "Button", parent, name, color);
        Place((RectTransform)button.transform, x, y, width, 44);
        Ref(controls, field, button);
    }

    // 값, 물리/논리 index, S/F 마커와 상태 테두리를 가진 슬롯 프리팹을 생성한다.
    private static DequeSlotUIView BuildSlot()
    {
        var root = Box("DequeSlotUI", null, Panel);
        Place(root.rectTransform, 0, 0, 112, 96);
        Fixed(root.gameObject, 112, 96);
        root.gameObject.AddComponent<CanvasGroup>();
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(2, -2);
        outline.enabled = false;
        var view = root.gameObject.AddComponent<DequeSlotUIView>();
        var index = Label("IndexText", root.transform, "slot 0 / i:0", 13);
        Place(index.rectTransform, 6, 3, 100, 20);
        var value = Label("ValueText", root.transform, "empty", 19);
        Place(value.rectTransform, 6, 24, 100, 28);
        value.resizeTextForBestFit = true;
        value.resizeTextMinSize = 11;
        value.resizeTextMaxSize = 19;
        // 빈 Deque에서는 같은 슬롯에 Start와 Finish가 함께 있으므로 마커 영역을 나눠 둔다.
        var start = Label("StartMarker", root.transform, "S", 16);
        Place(start.rectTransform, 6, 53, 44, 20);
        start.color = new Color(0.3f, 0.95f, 0.65f);
        var finish = Label("FinishMarker", root.transform, "F", 16);
        Place(finish.rectTransform, 62, 53, 44, 20);
        finish.color = new Color(1f, 0.6f, 0.35f);
        var state = Label("StateText", root.transform, "", 12);
        Place(state.rectTransform, 6, 75, 100, 18);
        Ref(view, "_indexText", index);
        Ref(view, "_valueText", value);
        Ref(view, "_startText", start);
        Ref(view, "_finishText", finish);
        Ref(view, "_stateText", state);
        Ref(view, "_background", root);
        Ref(view, "_outline", outline);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/DequeSlotUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<DequeSlotUIView>();
    }

    // 왼쪽 map 참조와 오른쪽 가로 슬롯 목록을 가진 행 프리팹을 생성한다.
    private static DequeMapRowUIView BuildRow(DequeSlotUIView slot)
    {
        var root = Rect("DequeMapRowUI", null);
        Place(root, 0, 0, 1150, 100);
        var layout = Row(root.gameObject, 12);
        layout.padding = new RectOffset(8, 8, 2, 2);
        var size = root.gameObject.AddComponent<LayoutElement>();
        size.minWidth = 1150;
        size.minHeight = 100;
        var view = root.gameObject.AddComponent<DequeMapRowUIView>();
        var head = Box("MapReference", root, Panel);
        Fixed(head.gameObject, 160, 96);
        var text = Label("MapText", head.transform, "[0] -> [] null", 18);
        Stretch(text.rectTransform, 10, 0, 4, 0);
        var slots = Rect("SlotRoot", root);
        Row(slots.gameObject, 6);
        Ref(view, "_mapText", text);
        Ref(view, "_slotRoot", slots);
        Ref(view, "_slotPrefab", slot);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/DequeMapRowUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<DequeMapRowUIView>();
    }

    // MAP 왼쪽에 Start와 Finish 카드를 세로로 배치하고 상세 정보 텍스트를 View에 연결한다.
    private static void BuildIterators(Transform parent, DequeUIView view)
    {
        string[] names = { "Start", "Finish" };
        string[] fields = { "_startText", "_finishText" };
        for (int i = 0; i < names.Length; i++)
        {
            var card = Box(names[i] + "Iterator", parent, Panel);
            Place(card.rectTransform, 44, 394 + i * 222, 240, 210);
            var title = Label("Title", card.transform, names[i].ToUpperInvariant(), 22);
            Place(title.rectTransform, 16, 8, 208, 32);
            title.color = i == 0 ? new Color(0.3f, 0.95f, 0.65f) : new Color(1f, 0.6f, 0.35f);
            var text = Label("IteratorText", card.transform, "", 17);
            Place(text.rectTransform, 16, 42, 208, 158);
            text.alignment = TextAnchor.UpperLeft;
            Ref(view, fields[i], text);
        }
    }

    // map 확장과 큰 block도 탐색할 수 있도록 가로·세로 스크롤 영역을 구성한다.
    private static void BuildScroll(Transform parent, DequeUIView view, DequeMapRowUIView row)
    {
        var panel = Box("MapScroll", parent, new Color(0.055f, 0.085f, 0.13f));
        Stretch(panel.rectTransform, 300, 394, 44, 62);
        var title = Label("Title", panel.transform, "MAP  ->  BLOCK  ->  SLOT", 18);
        Place(title.rectTransform, 12, 0, 800, 30);
        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32;
        var viewport = Box("Viewport", panel.transform, Color.white);
        Stretch(viewport.rectTransform, 4, 32, 4, 4);
        // 제목은 고정하고 viewport 밖으로 나간 map 행과 슬롯만 가린다.
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("MapRoot", viewport.transform);
        Place(content, 0, 0, 1150, 0);
        var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
        column.spacing = 2;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        // 행 개수와 슬롯 너비에 맞게 content를 늘려 ScrollRect가 실제 표시 범위를 알 수 있게 한다.
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        Ref(view, "_mapRoot", content);
        Ref(view, "_rowPrefab", row);
    }

    // 시작 시 사용할 타입 선택 패널을 만들고 Dropdown과 Start 버튼 참조를 연결한다.
    private static void BuildSetup(Transform parent, DequeControls controls)
    {
        var overlay = Box("TypeSelectionPanel", parent, new Color(0.01f, 0.02f, 0.04f, 0.94f));
        Stretch(overlay.rectTransform);
        var card = Box("Card", overlay.transform, Panel);
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        card.rectTransform.sizeDelta = new Vector2(680, 360);
        var title = Label("Title", card.transform, "Create a deque", 32);
        Place(title.rectTransform, 44, 24, 592, 52);
        var subtitle = Label("Subtitle", card.transform, "Choose a value type for this session.", 19);
        Place(subtitle.rectTransform, 44, 88, 592, 32);
        var label = Label("ValueLabel", card.transform, "VALUE TYPE", 16);
        Place(label.rectTransform, 44, 140, 592, 28);
        var type = Types("ValueTypeDropdown", card.transform);
        Place((RectTransform)type.transform, 44, 180, 592, 48);
        var start = Button("StartButton", card.transform, "Start", Accent);
        Place((RectTransform)start.transform, 44, 268, 592, 52);
        Ref(controls, "_setupPanel", overlay.gameObject);
        Ref(controls, "_valueTypeDropdown", type);
        Ref(controls, "_startButton", start);
    }
}
