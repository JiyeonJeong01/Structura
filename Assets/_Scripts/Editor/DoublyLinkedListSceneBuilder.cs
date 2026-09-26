using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static HashUIFactory;
using static DoublyLinkedListUIFactory;
using UISettings = DoublyLinkedListUISettings;
using UIRect = UnityEngine.Rect;

// 전용 씬과 프리팹을 생성하고 Controller/View/Controls를 직렬화 참조로 연결한다.
public static class DoublyLinkedListSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/DoublyLinkedListScene.unity";
    private const string PrefabPath = "Assets/_Projects/Prefabs/DoublyLinkedListNodeUI.prefab";

    // 부모 왼쪽 위를 기준으로 한 x, y, 너비, 높이. 기존 씬을 다시 만들 때 사용한다.
    private static readonly UIRect TitleBounds = new UIRect(44f, 24f, 1300f, 48f);
    private static readonly UIRect TypeBounds = new UIRect(44f, 78f, 720f, 28f);
    private static readonly UIRect StatsBounds = new UIRect(1030f, 78f, 526f, 28f);
    private static readonly UIRect FeedbackBounds = new UIRect(44f, 288f, 1512f, 32f);
    private static readonly UIRect OperationsBounds = new UIRect(44f, 130f, 1512f, 146f);
    private static readonly UIRect FindButtonBounds = new UIRect(0f, 0f, 100f, UISettings.ButtonHeight);
    private static readonly UIRect InputBounds = new UIRect(112f, 0f, 302f, UISettings.ButtonHeight);
    private static readonly UIRect InsertFirstBounds = new UIRect(430f, 0f, 164f, UISettings.ButtonHeight);
    private static readonly UIRect DeleteFirstBounds = new UIRect(970f, 0f, 174f, UISettings.ButtonHeight);
    private const float InsertButtonSpacing = 176f;
    private const float DeleteButtonSpacing = 180f;
    private static readonly string[] OperationNames = { "First", "Last", "Current" };
    private static readonly UIRect OperationHintBounds = new UIRect(0f, 64f, 1180f, 46f);
    private static readonly UIRect PreviousButtonBounds = new UIRect(1320f, 64f, 86f, UISettings.ButtonHeight);
    private static readonly UIRect NextButtonBounds = new UIRect(1420f, 64f, 86f, UISettings.ButtonHeight);
    private static readonly UIRect ListBounds = new UIRect(44f, 340f, 1512f, 480f);
    private static readonly UIRect EmptyBounds = new UIRect(300f, 290f, 912f, 44f);
    private static readonly UIRect SearchBounds = new UIRect(250f, 50f, 1012f, 48f);
    private static readonly UIRect LegendBounds = new UIRect(170f, 404f, 1172f, 40f);
    private static readonly UIRect SetupCardBounds = new UIRect(460f, 260f, 680f, 360f);
    private static readonly UIRect SetupTitleBounds = new UIRect(40f, 30f, 600f, 50f);
    private static readonly UIRect SetupHintBounds = new UIRect(40f, 96f, 600f, 38f);
    private static readonly UIRect SetupTypeBounds = new UIRect(40f, 170f, 600f, 48f);
    private static readonly UIRect StartButtonBounds = new UIRect(40f, 274f, 600f, UISettings.ButtonHeight);

    // 기존 씬과 수동 편집을 덮어쓰지 않고 최초 한 번만 생성한다.
    [MenuItem("Structura/Build DoublyLinkedList UI")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Debug.LogWarning("DoublyLinkedListScene already exists. Open it to edit the UI.");
            return;
        }
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = UISettings.BackgroundColor;
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UISettings.ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var background = Box("DoublyLinkedListUI", canvasObject.transform, UISettings.BackgroundColor);
        Stretch(background.rectTransform);
        var content = Rect("Content", background.transform);
        content.anchorMin = content.anchorMax = content.pivot = UISettings.CenterPivot;
        content.sizeDelta = UISettings.ReferenceResolution;
        var controls = content.gameObject.AddComponent<DoublyLinkedListControls>();
        var view = content.gameObject.AddComponent<DoublyLinkedListUIView>();
        var controller = new GameObject("DoublyLinkedListController").AddComponent<DoublyLinkedListController>();
        Ref(controller, "_controls", controls);
        Ref(controller, "_view", view);
        BuildHeader(content, controls);
        BuildOperations(content, controls);
        BuildList(content, view);
        var overlay = Box("MissOverlay", background.transform, UISettings.MissOverlayColor);
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = false;
        Ref(view, "_missOverlay", overlay);
        BuildSetup(content, controls);
        view.Refresh(new List<string>(), UISettings.NoSelection);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(scene, true);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    }

    // 제목과 타입/개수 표시를 기존 씬과 같은 영역에 둔다.
    private static void BuildHeader(Transform parent, DoublyLinkedListControls controls)
    {
        var title = Text("Title", parent, "STRUCTURA  /  DOUBLY LINKED LIST", UISettings.TitleFontSize);
        PlaceBounds(title.rectTransform, TitleBounds);
        var type = Text("Type", parent, "Choose a value type to begin.", UISettings.DetailFontSize);
        PlaceBounds(type.rectTransform, TypeBounds);
        var stats = Text("Stats", parent, $"Count: 0 / {UISettings.Capacity}", UISettings.DetailFontSize);
        PlaceBounds(stats.rectTransform, StatsBounds);
        stats.alignment = TextAlignmentOptions.Right;
        var feedback = Text("Feedback", parent, "Select a value type, then press Start.", UISettings.DetailFontSize);
        PlaceBounds(feedback.rectTransform, FeedbackBounds);
        Ref(controls, "_typeText", type);
        Ref(controls, "_statsText", stats);
        Ref(controls, "_feedbackText", feedback);
    }

    // Find, 입력, 삽입 세 개, 삭제 세 개를 한 줄에 놓고 선택 버튼은 아래 오른쪽에 둔다.
    private static void BuildOperations(Transform parent, DoublyLinkedListControls controls)
    {
        var root = Rect("Operations", parent);
        PlaceBounds(root, OperationsBounds);
        Ref(controls, "_operations", root.gameObject.AddComponent<CanvasGroup>());
        Ref(controls, "_findButton", MakeButton("Find", root, "Find", FindButtonBounds));
        var input = MakeInput(root);
        PlaceBounds((RectTransform)input.transform, InputBounds);
        Ref(controls, "_valueInput", input);
        var inserts = new Button[UISettings.OperationLocationCount];
        var deletes = new Button[UISettings.OperationLocationCount];
        for (int i = 0; i < OperationNames.Length; i++)
        {
            var insertBounds = InsertFirstBounds;
            insertBounds.x += i * InsertButtonSpacing;
            var deleteBounds = DeleteFirstBounds;
            deleteBounds.x += i * DeleteButtonSpacing;
            inserts[i] = MakeButton("Insert" + OperationNames[i], root, "Insert " + OperationNames[i], insertBounds);
            deletes[i] = MakeButton("Delete" + OperationNames[i], root, "Delete " + OperationNames[i], deleteBounds);
        }
        Refs(controls, "_insertButtons", inserts);
        Refs(controls, "_deleteButtons", deletes);
        var hint = Text("Hint", root, "Current: delete selected node / insert on its right link", UISettings.DetailFontSize);
        PlaceBounds(hint.rectTransform, OperationHintBounds);
        Ref(controls, "_previousButton", MakeButton("Previous", root, "<", PreviousButtonBounds));
        Ref(controls, "_nextButton", MakeButton("Next", root, ">", NextButtonBounds));
    }

    // 간선을 노드보다 먼저 배치해 선이 노드 위로 그려지지 않게 한다.
    private static void BuildList(Transform parent, DoublyLinkedListUIView view)
    {
        var panel = Box("ListPanel", parent, UISettings.ListPanelColor);
        PlaceBounds(panel.rectTransform, ListBounds);
        // 데이터 노드 N개와 양 끝 센티널 사이에는 N+1개의 연결이 필요하다.
        var links = new DoublyLinkedListLinkUIView[UISettings.Capacity + 1];
        for (int i = 0; i < links.Length; i++)
            links[i] = BuildLink(panel.transform, i);
        Refs(view, "_links", links);
        var nodeRoot = Rect("Nodes", panel.transform);
        Stretch(nodeRoot);
        Ref(view, "_nodeRoot", nodeRoot);
        var head = MakeNode("Head", nodeRoot, "HEAD");
        var tail = MakeNode("Tail", nodeRoot, "TAIL");
        head.Rect.anchoredPosition = new Vector2(UISettings.SentinelInset, UISettings.NodeRowY);
        tail.Rect.anchoredPosition = new Vector2(ListBounds.width - UISettings.SentinelInset, UISettings.NodeRowY);
        Ref(view, "_head", head);
        Ref(view, "_tail", tail);
        var template = MakeNode("DoublyLinkedListNodeUI", null, "value");
        var prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, PrefabPath);
        Object.DestroyImmediate(template.gameObject);
        Ref(view, "_nodePrefab", prefab.GetComponent<DoublyLinkedListNodeUIView>());
        var empty = Text("Empty", panel.transform, "Empty list  /  insert a value to begin", UISettings.EmptyFontSize);
        PlaceBounds(empty.rectTransform, EmptyBounds);
        empty.alignment = TextAlignmentOptions.Center;
        Ref(view, "_emptyText", empty);
        var search = Text("SearchValue", panel.transform, string.Empty, UISettings.SearchFontSize);
        PlaceBounds(search.rectTransform, SearchBounds);
        search.alignment = TextAlignmentOptions.Center;
        Ref(view, "_searchText", search);
        var legend = Text("Legend", panel.transform, "Top: next  >     Bottom: <  prev     Teal: active node + insertion link", UISettings.DetailFontSize);
        PlaceBounds(legend.rectTransform, LegendBounds);
        legend.alignment = TextAlignmentOptions.Center;
    }

    // 두 방향의 선 끝에 회전한 짧은 선 두 개로 화살촉을 만든다.
    private static DoublyLinkedListLinkUIView BuildLink(Transform parent, int index)
    {
        var root = Rect("Link " + index, parent);
        Stretch(root);
        var link = root.gameObject.AddComponent<DoublyLinkedListLinkUIView>();
        var graphics = new List<Graphic>();
        for (int direction = 0; direction < 2; direction++)
        {
            var line = Box(direction == 0 ? "Next" : "Previous", root, UISettings.TextColor);
            var rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = UISettings.TopLeftAnchor;
            rect.pivot = UISettings.LeftCenterPivot;
            rect.localRotation = Quaternion.Euler(0f, 0f, direction * UISettings.ReverseLinkAngle);
            line.raycastTarget = false;
            graphics.Add(line);
            for (int side = -1; side <= 1; side += 2)
            {
                var tip = Box("Arrow", rect, UISettings.TextColor);
                tip.rectTransform.anchorMin = tip.rectTransform.anchorMax = UISettings.RightCenterPivot;
                tip.rectTransform.pivot = UISettings.RightCenterPivot;
                tip.rectTransform.sizeDelta = new Vector2(UISettings.ArrowLength, UISettings.LinkThickness);
                tip.rectTransform.anchoredPosition = Vector2.zero;
                tip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * UISettings.ArrowAngle);
                tip.raycastTarget = false;
                graphics.Add(tip);
            }
            Ref(link, direction == 0 ? "_forward" : "_backward", rect);
        }
        Refs(link, "_graphics", graphics.ToArray());
        return link;
    }

    // Setup을 맨 위에 두고 Start 전까지 타입을 선택받는다.
    private static void BuildSetup(Transform parent, DoublyLinkedListControls controls)
    {
        var overlay = Box("SetupPanel", parent, UISettings.SetupOverlayColor);
        Stretch(overlay.rectTransform);
        var card = Box("Card", overlay.transform, UISettings.PanelColor);
        PlaceBounds(card.rectTransform, SetupCardBounds);
        var title = Text("Title", card.transform, "Create a doubly linked list", UISettings.TitleFontSize);
        PlaceBounds(title.rectTransform, SetupTitleBounds);
        var hint = Text("Hint", card.transform, $"Choose one value type. Up to {UISettings.Capacity} nodes.", UISettings.DefaultFontSize);
        PlaceBounds(hint.rectTransform, SetupHintBounds);
        var types = MakeTypes(card.transform);
        PlaceBounds((RectTransform)types.transform, SetupTypeBounds);
        Ref(controls, "_setupPanel", overlay.gameObject);
        Ref(controls, "_valueTypeDropdown", types);
        Ref(controls, "_startButton", MakeButton("Start", card.transform, "Start", StartButtonBounds));
    }
}
