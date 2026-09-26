using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReturnToLobbyScene : MonoBehaviour
{
    [SerializeField] private string _sceneName = "LobyScene";

    private Button _button;
    private GameObject _confirmationPanel;
    private Button _noButton;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(ShowConfirmation);
    }

    private void ShowConfirmation()
    {
        if (_confirmationPanel == null)
            CreateConfirmation();

        _confirmationPanel.transform.SetAsLastSibling();
        _confirmationPanel.SetActive(true);
        _noButton.Select();
    }

    private void CreateConfirmation()
    {
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        RectTransform overlay = CreateRect("ReturnConfirmation", canvas.transform, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        _confirmationPanel = overlay.gameObject;

        RectTransform panel = CreateRect("Panel", overlay, new Vector2(520f, 240f), Vector2.zero);
        panel.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.21f, 1f);
        CreateText("Message", panel, "Do you want to return to the main menu?",
            new Vector2(460f, 100f), new Vector2(0f, 40f));

        Button yesButton = CreateButton("Yes", panel, new Vector2(-110f, -65f));
        _noButton = CreateButton("No", panel, new Vector2(110f, -65f));
        yesButton.onClick.AddListener(LoadScene);
        _noButton.onClick.AddListener(HideConfirmation);

        // Keep keyboard/controller navigation inside the confirmation panel.
        yesButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = _noButton,
            selectOnRight = _noButton
        };
        _noButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = yesButton,
            selectOnRight = yesButton
        };
    }

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void CreateText(string objectName, Transform parent, string value, Vector2 size, Vector2 position)
    {
        RectTransform rect = CreateRect(objectName, parent, size, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static Button CreateButton(string label, Transform parent, Vector2 position)
    {
        RectTransform rect = CreateRect(label, parent, new Vector2(180f, 55f), position);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.25f, 0.35f, 0.5f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        CreateText("Label", rect, label, rect.sizeDelta, Vector2.zero);
        return button;
    }

    private void HideConfirmation()
    {
        _confirmationPanel.SetActive(false);
        _button.Select();
    }

    private void OnDisable()
    {
        if (_confirmationPanel != null)
            _confirmationPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(ShowConfirmation);
        if (_confirmationPanel != null)
            Destroy(_confirmationPanel);
    }

    private void LoadScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            Debug.LogError($"Scene '{_sceneName}' is missing from the active build scene list.", this);
            return;
        }

        SceneManager.LoadScene(_sceneName);
    }
}
