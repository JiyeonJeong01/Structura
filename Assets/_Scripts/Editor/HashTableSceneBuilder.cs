using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static HashUIFactory;

public static class HashTableSceneBuilder
{
    private const string PrefabFolder = "Assets/_Projects/Prefabs";

    // HashTableScene의 UI를 새로 구성하고 프리팹과 Controller 참조를 연결해 저장한다.
    [MenuItem("Structura/Build HashTable UI")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/HashTableScene.unity")
            throw new System.InvalidOperationException("Open HashTableScene first.");
        System.IO.Directory.CreateDirectory(PrefabFolder);
        var entry = BuildEntry();
        var bucket = BuildBucket(entry);

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
        }
        // 카메라, 조명, EventSystem은 유지하고 Canvas 내부 UI만 다시 생성한다.
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        var background = Box("HashTableUI", canvas.transform, new Color(0.035f, 0.06f, 0.10f));
        Stretch(background.rectTransform);
        var controls = background.gameObject.AddComponent<HashTableControls>();
        var view = background.gameObject.AddComponent<HashTableUIView>();
        var title = Label("Title", background.transform, "STRUCTURA  /  HASH TABLE", 30);
        Place(title.rectTransform, 44, 24, 1100, 48);
        var types = Label("SelectedTypes", background.transform, "Choose your key and value types to begin.", 18);
        Place(types.rectTransform, 44, 78, 1100, 30);
        Ref(controls, "_typeText", types);

        var operationRoot = Rect("Operations", background.transform);
        Stretch(operationRoot);
        var group = operationRoot.gameObject.AddComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        Ref(controls, "_operations", group);
        var key = Input("KeyInput", operationRoot, "Key");
        var value = Input("ValueInput", operationRoot, "Value");
        Place((RectTransform)key.transform, 44, 128, 330, 48);
        Place((RectTransform)value.transform, 390, 128, 330, 48);
        Ref(controls, "_keyInput", key);
        Ref(controls, "_valueInput", value);
        string[] names = { "Insert", "Remove", "Find", "Clear" };
        string[] fields = { "_insertButton", "_removeButton", "_findButton", "_clearButton" };
        for (int i = 0; i < names.Length; i++)
        {
            var button = Button(names[i] + "Button", operationRoot, names[i], i == 0 ? Accent : Panel);
            Place((RectTransform)button.transform, 748 + i * 150, 128, 134, 48);
            Ref(controls, fields[i], button);
        }
        var stats = Label("Stats", background.transform, "Count: 0    Capacity: -", 18);
        Place(stats.rectTransform, 44, 192, 1000, 30);
        var feedback = Label("Feedback", background.transform, "Select types, then press Start.", 20);
        Place(feedback.rectTransform, 44, 229, 1450, 32);
        Ref(controls, "_statsText", stats);
        Ref(controls, "_feedbackText", feedback);
        BuildScroll(background.transform, view, bucket);
        var hint = Label("Hint", background.transform, "HEAD -> first entry   |   Fixed slots, left to right   |   Scroll to explore   |   Auto rehash at 75% load", 17);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0, 0);
        hint.rectTransform.pivot = Vector2.zero;
        hint.rectTransform.anchoredPosition = new Vector2(44, 18);
        hint.rectTransform.sizeDelta = new Vector2(1450, 30);
        BuildSetup(background.transform, controls);

        var controller = Object.FindFirstObjectByType<HashTableController>();
        if (controller == null) controller = new GameObject("HashTableController").AddComponent<HashTableController>();
        Ref(controller, "_tableView", view);
        Ref(controller, "_controls", controls);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    // 키, 값, 다음 포인터 표시를 가진 엔트리 프리팹을 생성한다.
    private static HashEntryUIView BuildEntry()
    {
        var root = Box("HashEntryUI", null, new Color(0.12f, 0.19f, 0.28f));
        Place(root.rectTransform, 0, 0, 248, 72);
        var view = root.gameObject.AddComponent<HashEntryUIView>();
        var key = Label("KeyText", root.transform, "key", 20);
        var value = Label("ValueText", root.transform, "value", 17);
        var next = Label("NextPointer", root.transform, "-> null", 18);
        Place(key.rectTransform, 14, 7, 162, 28);
        Place(value.rectTransform, 14, 36, 162, 27);
        Place(next.rectTransform, 184, 0, 64, 72);
        value.color = new Color(0.6f, 0.76f, 0.85f);
        Ref(view, "_keyText", key);
        Ref(view, "_valueText", value);
        Ref(view, "_nextText", next);
        Ref(view, "_background", root);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/HashEntryUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<HashEntryUIView>();
    }

    // HEAD와 고정 슬롯 정렬 루트, 자유 이동 루트를 가진 버킷 프리팹을 생성한다.
    private static HashBucketUIView BuildBucket(HashEntryUIView entry)
    {
        var root = Rect("HashBucketUI", null);
        Place(root, 0, 0, 1450, 84);
        var row = Row(root.gameObject, 16);
        row.padding = new RectOffset(8, 8, 6, 6);
        var minimum = root.gameObject.AddComponent<LayoutElement>();
        minimum.minWidth = 1450;
        var view = root.gameObject.AddComponent<HashBucketUIView>();
        var head = Box("Head", root, Panel);
        Fixed(head.gameObject, 140, 72);
        var text = Label("HeadText", head.transform, "[0] HEAD\n-> null", 19);
        Stretch(text.rectTransform, 12, 0, 8, 0);
        var entryRoot = Rect("EntryLayoutRoot", root);
        Row(entryRoot.gameObject, 12);
        var slot = Rect("SlotTemplate", entryRoot);
        Fixed(slot.gameObject, 248, 72);
        slot.gameObject.SetActive(false);
        var effect = Rect("EffectRoot", root);
        Stretch(effect);
        // 자유 이동 루트가 HEAD와 슬롯의 수평 배치에 영향을 주지 않게 제외한다.
        effect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Ref(view, "_headText", text);
        Ref(view, "_entryLayoutRoot", entryRoot);
        Ref(view, "_effectRoot", effect);
        Ref(view, "_slotTemplate", slot);
        Ref(view, "_entryPrefab", entry);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/HashBucketUI.prefab");
        Object.DestroyImmediate(root.gameObject);
        return prefab.GetComponent<HashBucketUIView>();
    }

    // 용량 증가와 긴 충돌 체인을 모두 탐색할 수 있는 가로·세로 스크롤을 구성한다.
    private static void BuildScroll(Transform parent, HashTableUIView view, HashBucketUIView bucket)
    {
        var panel = Box("TableScroll", parent, new Color(0.055f, 0.085f, 0.13f));
        Stretch(panel.rectTransform, 44, 284, 44, 62);
        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32;
        var viewport = Box("Viewport", panel.transform, Color.white);
        Stretch(viewport.rectTransform, 4, 4, 4, 4);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("BucketRoot", viewport.transform);
        Place(content, 0, 0, 1450, 0);
        var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
        column.spacing = 8;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        Ref(view, "_bucketRoot", content);
        Ref(view, "_bucketPrefab", bucket);
    }

    // 시작 시 표시할 타입 선택 패널을 만들고 드롭다운과 Start 버튼을 연결한다.
    private static void BuildSetup(Transform parent, HashTableControls controls)
    {
        var overlay = Box("TypeSelectionPanel", parent, new Color(0.01f, 0.02f, 0.04f, 0.94f));
        Stretch(overlay.rectTransform);
        var card = Box("Card", overlay.transform, Panel);
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        card.rectTransform.sizeDelta = new Vector2(680, 400);
        var title = Label("Title", card.transform, "Create a hash table", 32);
        Place(title.rectTransform, 44, 32, 600, 52);
        var subtitle = Label("Subtitle", card.transform, "Choose the data types for this session.", 19);
        Place(subtitle.rectTransform, 44, 93, 600, 32);
        var keyLabel = Label("KeyLabel", card.transform, "KEY TYPE", 16);
        var valueLabel = Label("ValueLabel", card.transform, "VALUE TYPE", 16);
        Place(keyLabel.rectTransform, 44, 154, 270, 28);
        Place(valueLabel.rectTransform, 364, 154, 270, 28);
        var key = Types("KeyTypeDropdown", card.transform);
        var value = Types("ValueTypeDropdown", card.transform);
        Place((RectTransform)key.transform, 44, 192, 272, 48);
        Place((RectTransform)value.transform, 364, 192, 272, 48);
        var start = Button("StartButton", card.transform, "Start", Accent);
        Place((RectTransform)start.transform, 44, 292, 592, 56);
        Ref(controls, "_setupPanel", overlay.gameObject);
        Ref(controls, "_keyTypeDropdown", key);
        Ref(controls, "_valueTypeDropdown", value);
        Ref(controls, "_startButton", start);
    }
}
