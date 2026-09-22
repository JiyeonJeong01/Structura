using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>현재 HashTableScene의 Legacy UGUI 텍스트·입력·드롭다운을 TMP로 교체하고 참조를 다시 연결한다.</summary>
public static class HashTableTmpMigration
{
    private const string ScenePath = "Assets/Scenes/HashTableScene.unity";

    // 현재 열려 있는 HashTableScene만 변환해 사용자가 조정한 다른 씬과 프리팹을 건드리지 않는다.
    [MenuItem("Structura/Migrate HashTable UI to TMP")]
    public static void MigrateActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open HashTableScene before migrating its UI to TMP.");
            return;
        }

        var controls = Object.FindFirstObjectByType<HashTableControls>();
        if (controls == null)
        {
            Debug.LogError("HashTableControls was not found in the active scene.");
            return;
        }

        var resources = CreateResources();
        var inputFields = ReplaceInputFields(resources);
        var dropdowns = ReplaceDropdowns(resources);
        var textByName = ReplaceSceneTexts();

        if (!ReconnectControls(controls, inputFields, dropdowns, textByName))
            return;

        MigrateEntryPrefab();
        MigrateBucketPrefab();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("HashTable UI migrated to TMP.");
    }

    // TMP 기본 입력·드롭다운이 사용하는 기본 sprite 묶음을 구성한다.
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

    // 기존 InputField의 루트 RectTransform과 입력 설정을 보존한 TMP_InputField를 같은 위치에 만든다.
    private static Dictionary<string, TMP_InputField> ReplaceInputFields(TMP_DefaultControls.Resources resources)
    {
        var replacements = new Dictionary<string, TMP_InputField>();
        var legacyInputs = Object.FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var legacy in legacyInputs)
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

        // 이전 실행이 중간에 멈췄어도 이미 생성된 TMP 입력 필드를 다시 참조한다.
        foreach (var input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[input.gameObject.name] = input;

        return replacements;
    }

    // 기존 Dropdown의 선택 항목과 루트 위치를 보존한 TMP_Dropdown을 같은 부모 아래에 만든다.
    private static Dictionary<string, TMP_Dropdown> ReplaceDropdowns(TMP_DefaultControls.Resources resources)
    {
        var replacements = new Dictionary<string, TMP_Dropdown>();
        var legacyDropdowns = Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var legacy in legacyDropdowns)
        {
            var replacement = TMP_DefaultControls.CreateDropdown(resources);
            CopyControlTransform(legacy.gameObject, replacement);
            replacement.name = legacy.name;

            var dropdown = replacement.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            foreach (var option in legacy.options)
            {
                // 현재 TMP 버전은 옵션 텍스트와 아이콘 외에 텍스트 색도 생성자에서 받는다.
                dropdown.options.Add(new TMP_Dropdown.OptionData(option.text, option.image, Color.white));
            }
            dropdown.value = legacy.value;
            dropdown.interactable = legacy.interactable;
            CopyImageColor(legacy.GetComponent<Image>(), replacement.GetComponent<Image>());

            replacements[replacement.name] = dropdown;
            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        // 이전 실행이 중간에 멈췄어도 이미 생성된 TMP 드롭다운을 다시 참조한다.
        foreach (var dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[dropdown.gameObject.name] = dropdown;

        return replacements;
    }

    // 컨트롤 내부 텍스트를 제외한 Legacy Text를 같은 GameObject에서 TextMeshProUGUI로 교체한다.
    private static Dictionary<string, TMP_Text> ReplaceSceneTexts()
    {
        var replacements = new Dictionary<string, TMP_Text>();
        var legacyTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var legacy in legacyTexts)
        {
            var replacement = ReplaceText(legacy);
            replacements[replacement.gameObject.name] = replacement;
        }

        // 변환된 텍스트도 포함해야 중간 실패 뒤 다시 실행할 수 있다.
        foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            replacements[text.gameObject.name] = text;

        return replacements;
    }

    // 변경된 TMP 타입에 맞춰 HashTableControls의 직렬화 참조를 다시 연결한다.
    private static bool ReconnectControls(HashTableControls controls,
        Dictionary<string, TMP_InputField> inputFields,
        Dictionary<string, TMP_Dropdown> dropdowns,
        Dictionary<string, TMP_Text> texts)
    {
        if (!inputFields.TryGetValue("KeyInput", out var keyInput)
            || !inputFields.TryGetValue("ValueInput", out var valueInput)
            || !dropdowns.TryGetValue("KeyTypeDropdown", out var keyDropdown)
            || !dropdowns.TryGetValue("ValueTypeDropdown", out var valueDropdown)
            || !texts.TryGetValue("SelectedTypes", out var typeText)
            || !texts.TryGetValue("Stats", out var statsText)
            || !texts.TryGetValue("Feedback", out var feedbackText))
        {
            Debug.LogError("HashTable TMP migration could not find one or more named controls.");
            return false;
        }

        var serialized = new SerializedObject(controls);
        serialized.FindProperty("_keyTypeDropdown").objectReferenceValue = keyDropdown;
        serialized.FindProperty("_valueTypeDropdown").objectReferenceValue = valueDropdown;
        serialized.FindProperty("_keyInput").objectReferenceValue = keyInput;
        serialized.FindProperty("_valueInput").objectReferenceValue = valueInput;
        serialized.FindProperty("_typeText").objectReferenceValue = typeText;
        serialized.FindProperty("_statsText").objectReferenceValue = statsText;
        serialized.FindProperty("_feedbackText").objectReferenceValue = feedbackText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // HashEntryUI 프리팹의 세 텍스트를 TMP로 바꾸고 HashEntryUIView 필드에 연결한다.
    private static void MigrateEntryPrefab()
    {
        const string prefabPath = "Assets/_Projects/Prefabs/HashEntryUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var entry = root.GetComponent<HashEntryUIView>();
            var texts = ReplacePrefabTexts(root);
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("_keyText").objectReferenceValue = texts["KeyText"];
            serialized.FindProperty("_valueText").objectReferenceValue = texts["ValueText"];
            serialized.FindProperty("_nextText").objectReferenceValue = texts["NextPointer"];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // HashBucketUI 프리팹의 HEAD 텍스트를 TMP로 바꾸고 HashBucketUIView 필드에 연결한다.
    private static void MigrateBucketPrefab()
    {
        const string prefabPath = "Assets/_Projects/Prefabs/HashBucketUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var bucket = root.GetComponent<HashBucketUIView>();
            var texts = ReplacePrefabTexts(root);
            var serialized = new SerializedObject(bucket);
            serialized.FindProperty("_headText").objectReferenceValue = texts["HeadText"];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 프리팹 내부의 Legacy Text를 모두 TMP로 바꾸고 이름으로 결과를 찾을 수 있게 반환한다.
    private static Dictionary<string, TMP_Text> ReplacePrefabTexts(GameObject root)
    {
        var replacements = new Dictionary<string, TMP_Text>();
        foreach (var legacy in root.GetComponentsInChildren<Text>(true))
        {
            var replacement = ReplaceText(legacy);
            replacements[replacement.gameObject.name] = replacement;
        }

        // 이미 TMP로 변환된 프리팹도 같은 방법으로 다시 연결할 수 있게 한다.
        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            replacements[text.gameObject.name] = text;

        return replacements;
    }

    // 텍스트 내용과 기본 시각 속성을 보존하면서 같은 GameObject의 Legacy Text만 TMP로 교체한다.
    private static TMP_Text ReplaceText(Text legacy)
    {
        var gameObject = legacy.gameObject;
        var text = legacy.text;
        var color = legacy.color;
        var fontSize = legacy.fontSize;
        var alignment = legacy.alignment;
        var horizontalOverflow = legacy.horizontalOverflow;
        var verticalOverflow = legacy.verticalOverflow;

        // Graphic은 한 GameObject에 하나만 둘 수 있으므로 Legacy Text를 먼저 제거해야 한다.
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

    // 생성된 TMP 컨트롤이 기존 컨트롤의 부모, 순서, anchor, pivot, 크기를 그대로 사용하게 복사한다.
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

    // Legacy InputField의 콘텐츠 타입을 대응되는 TMP_InputField 타입으로 옮긴다.
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

    // Legacy TextAnchor를 TMP의 가장 가까운 정렬 옵션으로 바꾼다.
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

    // 기존 배경색은 유지해 TMP 기본 컨트롤 생성 뒤 시각적 변화가 과하지 않게 한다.
    private static void CopyImageColor(Image source, Image destination)
    {
        if (source != null && destination != null)
            destination.color = source.color;
    }
}
