using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HashUIFactory
{
    public static readonly Color Ink = new Color(0.87f, 0.93f, 0.98f);
    public static readonly Color Panel = new Color(0.08f, 0.12f, 0.18f);
    public static readonly Color Accent = new Color(0.13f, 0.64f, 0.55f);
    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // 지정한 부모 아래에 UI 레이어의 RectTransform 오브젝트를 생성한다.
    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    // 부모의 왼쪽 위를 기준으로 위치와 고정 크기를 지정한다.
    public static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    // 부모 전체에 맞춰 늘리되 지정한 네 방향 여백을 남긴다.
    public static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    // 배경이나 카드에 사용할 단색 Image를 생성한다.
    public static Image Box(string name, Transform parent, Color color)
    {
        var image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // 공통 글꼴과 색을 적용한 표시용 텍스트를 생성한다.
    public static Text Label(string name, Transform parent, string value, int size = 20)
    {
        var label = Rect(name, parent).gameObject.AddComponent<Text>();
        label.font = Font;
        label.text = value;
        label.fontSize = size;
        label.color = Ink;
        // 사용자가 입력한 꺾쇠괄호 등을 서식 태그로 해석하지 않는다.
        label.supportRichText = false;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    // 자식 개수에 따라 늘거나 줄지 않는 고정 크기의 레이아웃 요소를 추가한다.
    public static LayoutElement Fixed(GameObject go, float width, float height)
    {
        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = width;
        layout.minHeight = layout.preferredHeight = height;
        layout.flexibleWidth = layout.flexibleHeight = 0;
        return layout;
    }

    // 자식을 왼쪽부터 일정한 간격으로 배치하고 남는 공간으로 강제 확장하지 않는다.
    public static HorizontalLayoutGroup Row(GameObject go, float spacing)
    {
        var row = go.AddComponent<HorizontalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleLeft;
        row.spacing = spacing;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        return row;
    }

    // Unity 기본 UI의 부모와 이름을 설정하고 비활성 템플릿까지 공통 글꼴을 적용한다.
    private static T DefaultControl<T>(GameObject go, string name, Transform parent) where T : Component
    {
        go.name = name;
        go.transform.SetParent(parent, false);
        foreach (var text in go.GetComponentsInChildren<Text>(true))
        {
            text.font = Font;
            text.fontSize = 20;
            text.color = Panel;
            text.supportRichText = false;
        }
        return go.GetComponent<T>();
    }

    // 공통 스타일과 안내 문구를 가진 입력 필드를 생성한다.
    public static InputField Input(string name, Transform parent, string placeholder)
    {
        var input = DefaultControl<InputField>(DefaultControls.CreateInputField(new DefaultControls.Resources()), name, parent);
        ((Text)input.placeholder).text = placeholder;
        return input;
    }

    // HashValueType 순서와 동일한 타입 선택 드롭다운을 생성한다.
    public static Dropdown Types(string name, Transform parent)
    {
        var dropdown = DefaultControl<Dropdown>(DefaultControls.CreateDropdown(new DefaultControls.Resources()), name, parent);
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string> { "int", "float", "string" });
        var arrow = dropdown.transform.Find("Arrow");
        arrow.GetComponent<Image>().enabled = false;
        var arrowText = Label("ArrowText", arrow, "v", 18);
        Stretch(arrowText.rectTransform);
        arrowText.color = Panel;
        arrowText.alignment = TextAnchor.MiddleCenter;
        var item = dropdown.template.GetComponentInChildren<Toggle>(true);
        item.graphic.color = Accent;
        // 기본 항목 높이는 글자보다 작으므로 펼친 목록에서 잘리지 않도록 높인다.
        var itemRect = (RectTransform)item.transform;
        itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 36);
        var content = (RectTransform)itemRect.parent;
        content.sizeDelta = new Vector2(content.sizeDelta.x, 44);
        return dropdown;
    }

    // 공통 스타일의 버튼을 생성하고 문구와 배경색을 지정한다.
    public static Button Button(string name, Transform parent, string caption, Color color)
    {
        var button = DefaultControl<Button>(DefaultControls.CreateButton(new DefaultControls.Resources()), name, parent);
        button.image.color = color;
        var text = button.GetComponentInChildren<Text>();
        text.text = caption;
        text.color = Ink;
        return button;
    }

    // private 직렬화 필드에 씬 오브젝트 또는 프리팹 참조를 연결한다.
    public static void Ref(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

