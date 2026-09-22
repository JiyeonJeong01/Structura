using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>타입 선택과 입력을 표시하고 버튼을 Controller에 연결한다.</summary>
public class DequeControls : MonoBehaviour
{
    public ValueType ValueType => (ValueType)_valueTypeDropdown.value;
    public string ValueInput => _valueInput.text;
    public string GetAndRemoveAtInput => _getAndRemoveAtInput.text;
    public string SetAndInsertAtInput => _setAndInsertAtInput.text;
    public string SetAndInsertAtValueInput => _setAndInsertAtValueInput.text;
    public bool HasSetValueInput => !string.IsNullOrWhiteSpace(_setAndInsertAtValueInput.text);

    // Setup 패널 UI
    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private TMP_Dropdown _valueTypeDropdown;

    // 타입 선택 전에는 모든 조작 영역의 입력을 차단한다.
    [SerializeField] private CanvasGroup _operations;

    // 기본 양끝 연산의 Value와 Index Access / Cost Demo가 공유하는 입력 UI
    [SerializeField] private TMP_InputField _valueInput;
    [SerializeField] private TMP_InputField _getAndRemoveAtInput;
    [SerializeField] private TMP_InputField _setAndInsertAtInput;
    [SerializeField] private TMP_InputField _setAndInsertAtValueInput;

    // 세션 시작 및 기본 양끝 연산 Button UI
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _pushFrontButton;
    [SerializeField] private Button _pushBackButton;
    [SerializeField] private Button _popFrontButton;
    [SerializeField] private Button _popBackButton;
    [SerializeField] private Button _clearButton;

    // Index Access와 Cost Demo Button UI
    [SerializeField] private Button _getButton;
    [SerializeField] private Button _setButton;
    [SerializeField] private Button _insertAtButton;
    [SerializeField] private Button _removeAtButton;
    [SerializeField] private TMP_Text _getRemoveAtText;

    // 선택 타입, 자료구조 통계, 조작 결과 텍스트 UI
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _feedbackText;

    private DequeController _controller;

    // 버튼 클릭 시 호출할 Controller 함수를 연결한다. 입력 해석과 데이터 변경은 Controller가 담당한다.
    public void Connect(DequeController owner)
    {
        _controller = owner;

        _startButton.onClick.AddListener(owner.Initialize);
        _pushFrontButton.onClick.AddListener(owner.PushFront);
        _pushBackButton.onClick.AddListener(owner.PushBack);
        _popFrontButton.onClick.AddListener(owner.PopFront);
        _popBackButton.onClick.AddListener(owner.PopBack);
        _clearButton.onClick.AddListener(owner.Clear);

        _getButton.onClick.AddListener(owner.Get);
        _setButton.onClick.AddListener(owner.Set);

        _insertAtButton.onClick.AddListener(owner.InsertAt);
        _removeAtButton.onClick.AddListener(owner.RemoveAt);

        _getAndRemoveAtInput.onValueChanged.AddListener(ClearIndexValueOnIndexChanged);
    }

    // 이 Controls에서 등록한 클릭 이벤트만 해제한다.
    private void OnDestroy()
    {
        if (_controller == null) 
            return;

        _startButton.onClick.RemoveListener(_controller.Initialize);
        _pushFrontButton.onClick.RemoveListener(_controller.PushFront);
        _pushBackButton.onClick.RemoveListener(_controller.PushBack);
        _popFrontButton.onClick.RemoveListener(_controller.PopFront);
        _popBackButton.onClick.RemoveListener(_controller.PopBack);
        _clearButton.onClick.RemoveListener(_controller.Clear);

        _getButton.onClick.RemoveListener(_controller.Get);
        _setButton.onClick.RemoveListener(_controller.Set);

        _insertAtButton.onClick.RemoveListener(_controller.InsertAt);
        _removeAtButton.onClick.RemoveListener(_controller.RemoveAt);

        _getAndRemoveAtInput.onValueChanged.RemoveListener(ClearIndexValueOnIndexChanged);
    }

    // Get/RemoveAt 결과는 index 입력란이 아니라 결과 텍스트에 표시한다.
    public void SetGetAndRemoveAtValue(string value)
    {
        _getRemoveAtText.text = value;
    }

    // 타입 선택 패널과 조작 영역의 입력 가능 상태를 반대로 전환한다.
    public void ShowSetupPanel(bool show)
    {
        _setupPanel.SetActive(show);
        _operations.interactable = _operations.blocksRaycasts = !show;
    }

    // 선택한 타입에 맞는 입력 규칙을 적용하고 기본 Value 입력란에서 편집을 시작한다.
    public void StartShowDeque(ValueType type)
    {
        ShowSetupPanel(false);

        _typeText.text = $"VALUE : {type.ToString().ToLowerInvariant()}";
        _valueInput.text = _setAndInsertAtValueInput.text = string.Empty;
        _getAndRemoveAtInput.text = "0";
        _getRemoveAtText.text = string.Empty;
        _setAndInsertAtInput.text = "0";

        // index는 항상 정수다. 두 Value 입력란은 같은 타입 제한을 사용한다.
        _getAndRemoveAtInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        _setAndInsertAtInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // 타입 결정
        var contentType = type == ValueType.Int ? 
            TMP_InputField.ContentType.IntegerNumber
            : type == ValueType.Float ? TMP_InputField.ContentType.DecimalNumber : TMP_InputField.ContentType.Standard;

        _valueInput.contentType = _setAndInsertAtValueInput.contentType = contentType;
        _valueInput.Select();
        _valueInput.ActivateInputField();
    }

    // Controller가 계산한 Count, map 크기, block 크기를 표시한다.
    public void SetStats(string message)
    {
        _statsText.text = message;
    }

    // 성공 결과와 잘못된 입력 안내를 텍스트 색으로 구분한다.
    public void SetFeedback(string message, bool invalid)
    {
        _feedbackText.text = message;
        _feedbackText.color = invalid ? new Color(1f, 0.65f, 0.4f) : new Color(0.55f, 0.87f, 0.81f);
    }

    public void SetBusy(bool busy)
    {
        _operations.interactable = !busy;
        _operations.blocksRaycasts = !busy;
    }

    public void ClearGetAndRemoveAtValue()
    {
        _getRemoveAtText.text = string.Empty;
    }

    private void ClearIndexValueOnIndexChanged(string _)
    {
        ClearGetAndRemoveAtValue();
    }

}
