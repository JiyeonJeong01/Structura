using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>현재 DequeScene의 Legacy UGUI 텍스트·입력·드롭다운을 TMP로 바꾸고 참조를 다시 연결한다.</summary>
public static class DequeTmpMigration
{
    private const string ScenePath = "Assets/Scenes/DequeScene.unity";

    // 사용자가 구성한 DequeScene만 바꾸고 다른 자료구조 씬은 건드리지 않는다.
    [MenuItem("Structura/Migrate Deque UI to TMP")]
    public static void MigrateActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open DequeScene before migrating its UI to TMP.");
            return;
        }

        var controls = Object.FindFirstObjectByType<DequeControls>();
        var view = Object.FindFirstObjectByType<DequeUIView>();
        if (controls == null || view == null)
        {
            Debug.LogError("DequeControls or DequeUIView was not found in the active scene.");
            return;
        }

        var resources = CreateResources();
        var inputFields = ReplaceInputFields(resources);
        var dropdowns = ReplaceDropdowns(resources);
        var texts = ReplaceSceneTexts();

        if (!ReconnectControls(controls, inputFields, dropdowns, texts)
            || !ReconnectView(view, texts))
            return;

        MigrateSlotPrefab();
        MigrateMapRowPrefab();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Deque UI migrated to TMP.");
    }

    // TMP 기본 컨트롤이 사용하는 내장 sprite 묶음을 구성한다.
    private static TMP_DefaultControls.Resources CreateResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    // Legacy InputField의 위치와 입력 규칙을 보존한 TMP_InputField를 같은 부모에 만든다.
    private static Dictionary<string, TMP_InputField> ReplaceInputFields(TMP_DefaultControls.Resources resources)
    {
        var replacements = new Dictionary<string, TMP_InputField>();
        foreach (var legacy in Object.FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var replacement = TMP_DefaultControls.CreateInputField(resources);
            CopyControlTransform(legacy.gameObject, replacement);
            replacement.name = legacy.name;

            var input = replacement.GetComponent<TMP_InputField>();
            input.text = legacy.text;
            input.interactable = legacy.interactable;
            input.contentType = ToTmpContentType(legacy.contentType);
            input.characterLimit = legacy.characterLimit;
            CopyImageColor(legacy.GetComponent<Image>(), replacement.GetComponent<Image>());

            replacements[replacement.name] = input;
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        // 중간 실패 후 재실행해도 이미 만들어진 TMP 입력 필드를 다시 참조한다.
        foreach (var input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[input.gameObject.name] = input;

        return replacements;
    }

    // Legacy Dropdown의 항목과 위치를 유지한 TMP_Dropdown을 같은 부모에 만든다.
    private static Dictionary<string, TMP_Dropdown> ReplaceDropdowns(TMP_DefaultControls.Resources resources)
    {
        var replacements = new Dictionary<string, TMP_Dropdown>();
        foreach (var legacy in Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var replacement = TMP_DefaultControls.CreateDropdown(resources);
            CopyControlTransform(legacy.gameObject, replacement);
            replacement.name = legacy.name;

            var dropdown = replacement.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            foreach (var option in legacy.options)
                dropdown.options.Add(new TMP_Dropdown.OptionData(option.text, option.image, Color.white));
            dropdown.value = legacy.value;
            dropdown.interactable = legacy.interactable;
            CopyImageColor(legacy.GetComponent<Image>(), replacement.GetComponent<Image>());

            replacements[replacement.name] = dropdown;
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        // 중간 실패 후 재실행해도 이미 만들어진 TMP 드롭다운을 다시 참조한다.
        foreach (var dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[dropdown.gameObject.name] = dropdown;

        return replacements;
    }

    // 씬의 Legacy Text를 TMP로 교체하고 중복 이름을 피하기 위해 전체 계층 경로로 기록한다.
    private static Dictionary<string, TMP_Text> ReplaceSceneTexts()
    {
        var replacements = new Dictionary<string, TMP_Text>();
        foreach (var legacy in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var replacement = ReplaceText(legacy);
            replacements[GetHierarchyPath(replacement.transform)] = replacement;
        }

        foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[GetHierarchyPath(text.transform)] = text;

        return replacements;
    }

    // TMP 타입으로 바뀐 DequeControls의 입력·선택·표시 참조를 씬 오브젝트에 다시 연결한다.
    private static bool ReconnectControls(DequeControls controls,
        Dictionary<string, TMP_InputField> inputFields,
        Dictionary<string, TMP_Dropdown> dropdowns,
        Dictionary<string, TMP_Text> texts)
    {
        if (!inputFields.TryGetValue("ValueInput", out var valueInput)
            || !inputFields.TryGetValue("GetAndRemoveAtIndexInput", out var getAndRemoveAtInput)
            || !inputFields.TryGetValue("SetAndInsertAtIndexInput", out var setAndInsertAtInput)
            || !inputFields.TryGetValue("SetAndInsertAtValueInput", out var setAndInsertAtValueInput)
            || !dropdowns.TryGetValue("ValueTypeDropdown", out var valueTypeDropdown)
            || !texts.TryGetValue("/DequeCanvas/DequeUI/Operations/Get_RemoveAt/ExpressionValue", out var getRemoveAtText)
            || !texts.TryGetValue("/DequeCanvas/DequeUI/SelectedType", out var typeText)
            || !texts.TryGetValue("/DequeCanvas/DequeUI/Stats", out var statsText)
            || !texts.TryGetValue("/DequeCanvas/DequeUI/Feedback", out var feedbackText))
        {
            Debug.LogError("Deque TMP migration could not find one or more named controls.");
            return false;
        }

        var serialized = new SerializedObject(controls);
        serialized.FindProperty("_valueTypeDropdown").objectReferenceValue = valueTypeDropdown;
        serialized.FindProperty("_valueInput").objectReferenceValue = valueInput;
        serialized.FindProperty("_getAndRemoveAtInput").objectReferenceValue = getAndRemoveAtInput;
        serialized.FindProperty("_setAndInsertAtInput").objectReferenceValue = setAndInsertAtInput;
        serialized.FindProperty("_setAndInsertAtValueInput").objectReferenceValue = setAndInsertAtValueInput;
        serialized.FindProperty("_getRemoveAtText").objectReferenceValue = getRemoveAtText;
        serialized.FindProperty("_typeText").objectReferenceValue = typeText;
        serialized.FindProperty("_statsText").objectReferenceValue = statsText;
        serialized.FindProperty("_feedbackText").objectReferenceValue = feedbackText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // Start와 Finish는 같은 이름의 IteratorText를 사용하므로 전체 계층 경로로 구분해 연결한다.
    private static bool ReconnectView(DequeUIView view, Dictionary<string, TMP_Text> texts)
    {
        if (!texts.TryGetValue("/DequeCanvas/DequeUI/StartIterator/IteratorText", out var startText)
            || !texts.TryGetValue("/DequeCanvas/DequeUI/FinishIterator/IteratorText", out var finishText))
        {
            Debug.LogError("Deque TMP migration could not find the iterator texts.");
            return false;
        }

        var serialized = new SerializedObject(view);
        serialized.FindProperty("_startText").objectReferenceValue = startText;
        serialized.FindProperty("_finishText").objectReferenceValue = finishText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // DequeSlotUI의 다섯 표시 텍스트를 TMP로 바꾸고 SlotView의 참조를 다시 연결한다.
    private static void MigrateSlotPrefab()
    {
        const string prefabPath = "Assets/_Projects/Prefabs/DequeSlotUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var slot = root.GetComponent<DequeSlotUIView>();
            var texts = ReplacePrefabTexts(root);
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("_indexText").objectReferenceValue = texts["IndexText"];
            serialized.FindProperty("_valueText").objectReferenceValue = texts["ValueText"];
            serialized.FindProperty("_startText").objectReferenceValue = texts["StartMarker"];
            serialized.FindProperty("_finishText").objectReferenceValue = texts["FinishMarker"];
            serialized.FindProperty("_stateText").objectReferenceValue = texts["StateText"];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // DequeMapRowUI의 map 참조 텍스트를 TMP로 바꾸고 MapRowView의 참조를 다시 연결한다.
    private static void MigrateMapRowPrefab()
    {
        const string prefabPath = "Assets/_Projects/Prefabs/DequeMapRowUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var row = root.GetComponent<DequeMapRowUIView>();
            var texts = ReplacePrefabTexts(root);
            var serialized = new SerializedObject(row);
            serialized.FindProperty("_mapText").objectReferenceValue = texts["MapText"];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 프리팹 안의 Legacy Text를 모두 TMP로 바꾸고 오브젝트 이름으로 찾을 수 있게 반환한다.
    private static Dictionary<string, TMP_Text> ReplacePrefabTexts(GameObject root)
    {
        var replacements = new Dictionary<string, TMP_Text>();
        foreach (var legacy in root.GetComponentsInChildren<Text>(true))
        {
            var replacement = ReplaceText(legacy);
            replacements[replacement.gameObject.name] = replacement;
        }

        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            replacements[text.gameObject.name] = text;

        return replacements;
    }

    // Legacy Text의 시각 속성을 보관한 뒤 제거해야 하나의 GameObject에 Graphic이 겹치지 않는다.
    private static TMP_Text ReplaceText(Text legacy)
    {
        var gameObject = legacy.gameObject;
        var text = legacy.text;
        var color = legacy.color;
        var fontSize = legacy.fontSize;
        var alignment = legacy.alignment;
        var horizontalOverflow = legacy.horizontalOverflow;
        var verticalOverflow = legacy.verticalOverflow;

        Undo.DestroyObjectImmediate(legacy);
        var replacement = Undo.AddComponent<TextMeshProUGUI>(gameObject);
        replacement.text = text;
        replacement.color = color;
        replacement.fontSize = fontSize;
        replacement.alignment = ToTmpAlignment(alignment);
        replacement.textWrappingMode = horizontalOverflow == HorizontalWrapMode.Wrap
            ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        replacement.overflowMode = verticalOverflow == VerticalWrapMode.Overflow
            ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        return replacement;
    }

    // TMP 기본 컨트롤이 기존 컨트롤의 부모·순서·RectTransform을 그대로 사용하게 복사한다.
    private static void CopyControlTransform(GameObject source, GameObject destination)
    {
        var sourceRect = source.GetComponent<RectTransform>();
        var destinationRect = destination.GetComponent<RectTransform>();
        destination.transform.SetParent(source.transform.parent, false);
        destination.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        destinationRect.anchorMin = sourceRect.anchorMin;
        destinationRect.anchorMax = sourceRect.anchorMax;
        destinationRect.pivot = sourceRect.pivot;
        destinationRect.anchoredPosition = sourceRect.anchoredPosition;
        destinationRect.sizeDelta = sourceRect.sizeDelta;
        destinationRect.localRotation = sourceRect.localRotation;
        destinationRect.localScale = sourceRect.localScale;
        destination.SetActive(source.activeSelf);
    }

    // Legacy InputField 콘텐츠 타입을 TMP의 같은 의미의 콘텐츠 타입으로 바꾼다.
    private static TMP_InputField.ContentType ToTmpContentType(InputField.ContentType contentType)
    {
        switch (contentType)
        {
            case InputField.ContentType.IntegerNumber:
                return TMP_InputField.ContentType.IntegerNumber;
            case InputField.ContentType.DecimalNumber:
                return TMP_InputField.ContentType.DecimalNumber;
            default:
                return TMP_InputField.ContentType.Standard;
        }
    }

    // Legacy TextAnchor를 TMP에서 가장 가까운 정렬 옵션으로 변환한다.
    private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            default:
                return TextAlignmentOptions.BottomRight;
        }
    }

    // 씬 텍스트의 중복된 이름을 피하기 위해 씬 루트부터의 전체 경로를 반환한다.
    private static string GetHierarchyPath(Transform target)
    {
        var path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return "/" + path;
    }

    // 기존 배경색을 유지해 TMP 기본 컨트롤로 바꾼 뒤 색 변화가 커지지 않게 한다.
    private static void CopyImageColor(Image source, Image destination)
    {
        if (source != null && destination != null)
            destination.color = source.color;
    }
}
