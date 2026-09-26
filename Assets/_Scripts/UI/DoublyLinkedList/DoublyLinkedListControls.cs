using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UISettings = DoublyLinkedListUISettings;

// 타입 선택과 입력 상태를 관리하며 리스트 로직은 Controller에 맡긴다.
public class DoublyLinkedListControls : MonoBehaviour
{
    public ValueType ValueType => (ValueType)_valueTypeDropdown.value;
    public string ValueInput => _valueInput.text;
    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private TMP_Dropdown _valueTypeDropdown;
    [SerializeField] private TMP_InputField _valueInput;
    [SerializeField] private CanvasGroup _operations;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _findButton;
    [SerializeField] private Button[] _insertButtons;
    [SerializeField] private Button[] _deleteButtons;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _clearButton;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _feedbackText;
    private DoublyLinkedListController _controller;
    private readonly UnityAction[] _insertActions = new UnityAction[UISettings.OperationLocationCount];
    private readonly UnityAction[] _deleteActions = new UnityAction[UISettings.OperationLocationCount];
    private bool _replaceValueOnNextEdit;

    // 해제할 수 있는 delegate를 보관해 버튼마다 연산 위치를 연결한다.
    public void Connect(DoublyLinkedListController controller)
    {
        _controller = controller;
        _startButton.onClick.AddListener(controller.Initialize);
        _findButton.onClick.AddListener(controller.Find);
        _previousButton.onClick.AddListener(Previous);
        _nextButton.onClick.AddListener(Next);
        _valueInput.onSelect.AddListener(PrepareRetainedValueForEdit);
        _clearButton.onClick.AddListener(controller.Clear);

        for (int i = 0; i < _insertActions.Length; i++)
        {
            int location = i;
            _insertActions[i] = () => controller.Insert(location);
            _deleteActions[i] = () => controller.Delete(location);
            _insertButtons[i].onClick.AddListener(_insertActions[i]);
            _deleteButtons[i].onClick.AddListener(_deleteActions[i]);
        }
    }

    // Setup이 보이는 동안 뒤쪽 조작 패널을 잠근다.
    public void ShowSetup(bool show)
    {
        _setupPanel.SetActive(show);
        _operations.interactable = _operations.blocksRaycasts = !show;
    }

    // 기존 자료구조와 동일한 타입별 TMP 입력 모드를 적용한다.
    public void Begin(ValueType type)
    {
        ShowSetup(false);

        _typeText.text = "VALUE TYPE  /  " + type.ToString().ToUpperInvariant();

        _valueInput.contentType = type == ValueType.Int ? TMP_InputField.ContentType.IntegerNumber
            : type == ValueType.Float ? TMP_InputField.ContentType.DecimalNumber
            : TMP_InputField.ContentType.Standard;
        _valueInput.text = string.Empty;
        _valueInput.ActivateInputField();
    }

    // 용량과 선택 경계에 맞춰 실제 실행 가능한 버튼만 활성화한다.
    public void SetState(bool started, bool busy, int count, int active)
    {
        bool ready = started && !busy;
        _operations.interactable = _operations.blocksRaycasts = ready;
        foreach (var button in _insertButtons)
            button.interactable = ready && count < DoublyLinkedListController.Capacity;
        foreach (var button in _deleteButtons)
            button.interactable = ready && count > 0;
        _previousButton.interactable = ready && active > 0;
        _nextButton.interactable = ready && active >= 0 && active < count - 1;
        _statsText.text = $"Count: {count} / {UISettings.Capacity}    Active: {(active < 0 ? "none" : "[" + active + "]")}";
    }

    // 정상 안내와 잘못된 입력을 색으로 구분한다.
    public void SetFeedback(string message, bool invalid = false)
    {
        _feedbackText.text = message;
        _feedbackText.color = invalid ? UISettings.InvalidFeedbackColor : UISettings.FeedbackColor;
    }

    // 연속 입력 시 이전 값을 한 번에 바꿀 수 있게 표시만 남긴다.
    public void RetainValueInput()
    {
        _replaceValueOnNextEdit = true;
    }

    // 이전 입력을 다시 클릭하면 전체 선택한다.
    private void PrepareRetainedValueForEdit(string value)
    {
        if (!_replaceValueOnNextEdit)
            return;
        _valueInput.selectionAnchorPosition = 0;
        _valueInput.selectionFocusPosition = _valueInput.text.Length;
        _replaceValueOnNextEdit = false;
    }

    // 왼쪽 선택 버튼의 리스너로 사용한다.
    private void Previous()
    {
        _controller.MoveSelection(-1);
    }

    // 오른쪽 선택 버튼의 리스너로 사용한다.
    private void Next()
    {
        _controller.MoveSelection(1);
    }

    // 이 컴포넌트가 등록한 이벤트만 해제한다.
    private void OnDestroy()
    {
        if (_controller == null)
            return;
        _startButton.onClick.RemoveListener(_controller.Initialize);
        _findButton.onClick.RemoveListener(_controller.Find);
        _previousButton.onClick.RemoveListener(Previous);
        _nextButton.onClick.RemoveListener(Next);
        _valueInput.onSelect.RemoveListener(PrepareRetainedValueForEdit);
        _clearButton.onClick.RemoveListener(_controller.Clear);
        for (int i = 0; i < _insertActions.Length; i++)
        {
            _insertButtons[i].onClick.RemoveListener(_insertActions[i]);
            _deleteButtons[i].onClick.RemoveListener(_deleteActions[i]);
        }
    }
}
