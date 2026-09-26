using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static HashUIFactory;
using UISettings = DoublyLinkedListUISettings;

// 리스트 씬에서 사용하는 TMP 컨트롤과 노드 프리팹을 만든다.
public static class DoublyLinkedListUIFactory
{
    private const float PointerCellWidth = 34f;
    private const float ValueCellPadding = 4f;
    private const float DividerWidth = 1f;
    private static readonly string[] ValueTypeNames = { "int", "float", "string" };

    // 사용자 문자열의 태그 해석을 끄고 긴 값은 말줄임표로 표시한다.
    public static TMP_Text Text(string name, Transform parent, string value, int size = UISettings.DefaultFontSize)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = UISettings.TextColor;
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    // 공통 색을 사용하되 글자는 TMP로 만든다.
    public static Button MakeButton(string name, Transform parent, string caption, UnityEngine.Rect bounds)
    {
        var image = Box(name, parent, UISettings.PanelColor);
        PlaceBounds(image.rectTransform, bounds);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var label = Text("Caption", image.transform, caption, UISettings.DetailFontSize);
        Stretch(label.rectTransform, UISettings.ButtonTextPadding, 0f, UISettings.ButtonTextPadding, 0f);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    // TMP 기본 UI 리소스를 사용해 입력과 드롭다운의 동작을 일관되게 유지한다.
    private static TMP_DefaultControls.Resources Resources()
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

    // 기본 입력 필드의 텍스트에도 공통 폰트를 지정한다.
    public static TMP_InputField MakeInput(Transform parent)
    {
        var go = TMP_DefaultControls.CreateInputField(Resources());
        go.name = "ValueInput";
        go.transform.SetParent(parent, false);
        ConfigureTexts(go);
        var input = go.GetComponent<TMP_InputField>();
        ((TMP_Text)input.placeholder).text = "Value";
        return input;
    }

    // ValueType enum과 같은 순서의 옵션을 생성한다.
    public static TMP_Dropdown MakeTypes(Transform parent)
    {
        var go = TMP_DefaultControls.CreateDropdown(Resources());
        go.name = "ValueTypeDropdown";
        go.transform.SetParent(parent, false);
        ConfigureTexts(go);
        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(ValueTypeNames));
        return dropdown;
    }

    // 숨겨진 드롭다운 항목까지 폰트와 글자 크기를 적용한다.
    private static void ConfigureTexts(GameObject go)
    {
        foreach (var text in go.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = UISettings.DefaultFontSize;
            text.richText = false;
        }
    }

    // 포인터 칸 너비를 제외한 가운데 공간에 값을 표시한다.
    public static DoublyLinkedListNodeUIView MakeNode(string name, Transform parent, string value)
    {
        var image = Box(name, parent, UISettings.NodeColor);
        var rect = image.rectTransform;
        Place(rect, 0f, 0f, UISettings.NodeSize.x, UISettings.NodeSize.y);
        rect.pivot = UISettings.CenterPivot;
        image.gameObject.AddComponent<CanvasGroup>();
        var outline = image.gameObject.AddComponent<Outline>();
        outline.enabled = false;
        var node = image.gameObject.AddComponent<DoublyLinkedListNodeUIView>();
        float rightCellX = UISettings.NodeSize.x - PointerCellWidth;
        float valueCellX = PointerCellWidth + ValueCellPadding;
        float valueCellWidth = rightCellX - valueCellX - ValueCellPadding;
        var left = Text("Left", rect, "prev", UISettings.PointerFontSize);
        Place(left.rectTransform, 0f, 0f, PointerCellWidth, UISettings.NodeSize.y);
        var center = Text("Value", rect, value, UISettings.NodeValueFontSize);
        Place(center.rectTransform, valueCellX, 0f, valueCellWidth, UISettings.NodeSize.y);
        var right = Text("Right", rect, "next", UISettings.PointerFontSize);
        Place(right.rectTransform, rightCellX, 0f, PointerCellWidth, UISettings.NodeSize.y);
        left.alignment = center.alignment = right.alignment = TextAlignmentOptions.Center;
        for (int i = 0; i < 2; i++)
        {
            var divider = Box("Divider", rect, UISettings.DividerColor);
            Place(divider.rectTransform, i == 0 ? PointerCellWidth : rightCellX - DividerWidth,
                0f, DividerWidth, UISettings.NodeSize.y);
            divider.raycastTarget = false;
        }
        Ref(node, "_valueText", center);
        Ref(node, "_leftText", left);
        Ref(node, "_rightText", right);
        Ref(node, "_outline", outline);
        node.SetValue(value, name == "Head", name == "Tail");
        return node;
    }

    // 이름을 붙인 배치 영역을 기존 공통 Place 함수에 전달한다.
    public static void PlaceBounds(RectTransform rect, UnityEngine.Rect bounds)
    {
        Place(rect, bounds.x, bounds.y, bounds.width, bounds.height);
    }

    // 배열 참조도 직렬화해서 씬을 열었을 때 즉시 연결되도록 한다.
    public static void Refs<T>(Object target, string field, T[] values) where T : Object
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
