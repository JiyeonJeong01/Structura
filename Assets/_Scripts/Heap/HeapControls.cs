using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Heap 타입 선택, 입력, 버튼 잠금 상태를 관리한다.</summary>
public class HeapControls : MonoBehaviour
{
    public ValueType ValueType => (ValueType)_valueTypeDropdown.value;
    public string ValueInput => _valueInput.text;

    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private TMP_Dropdown _valueTypeDropdown;
    [SerializeField] private CanvasGroup _operations;
    [SerializeField] private TMP_InputField _valueInput;

    [SerializeField] private Button _startButton;
    [SerializeField] private Button _pushButton;
    [SerializeField] private Button _popButton;
    [SerializeField] private Button _clearButton;
    [SerializeField] private Button _stepButton;
    [SerializeField] private Button _playButton;

    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _feedbackText;

    private HeapController _controller;
    private bool _replaceValueOnNextEdit;

    public void Connect(HeapController owner)
    {
        _controller = owner;
        
        // 이벤트 등록
        _startButton.onClick.AddListener(_controller.Initialize);
        _pushButton.onClick.AddListener(_controller.Push);
        _popButton.onClick.AddListener(_controller.Pop);
        _clearButton.onClick.AddListener(_controller.Clear);
        _stepButton.onClick.AddListener(_controller.Step);
        _playButton.onClick.AddListener(_controller.Play);
        _valueInput.onSelect.AddListener(PrepareRetainedValueForEdit);
    }

    private void OnDestroy()
    {
        if (_controller == null)
            return;

        // 이벤트 해제
        _startButton.onClick.RemoveListener(_controller.Initialize);
        _pushButton.onClick.RemoveListener(_controller.Push);
        _popButton.onClick.RemoveListener(_controller.Pop);
        _clearButton.onClick.RemoveListener(_controller.Clear);
        _stepButton.onClick.RemoveListener(_controller.Step);
        _playButton.onClick.RemoveListener(_controller.Play);
        _valueInput.onSelect.RemoveListener(PrepareRetainedValueForEdit);
    }

    public void ShowSetupPanel(bool show)
    {
        _setupPanel.SetActive(show);
        _operations.interactable = _operations.blocksRaycasts = !show;
    }

    public void StartShowHeap(ValueType type)
    {
        ShowSetupPanel(false);

        _typeText.text = $"VALUE TYPE  /  {type.ToString().ToUpperInvariant()}";
        _valueInput.text = string.Empty;
        _replaceValueOnNextEdit = false;

        // SetUp 패널에서 설정한 타입 저장
        _valueInput.contentType = type == ValueType.Int
            ? TMP_InputField.ContentType.IntegerNumber
            : type == ValueType.Float
                ? TMP_InputField.ContentType.DecimalNumber
                : TMP_InputField.ContentType.Standard;

        _valueInput.Select();
        _valueInput.ActivateInputField();
    }

    public void RetainValueInput()
    {
        _replaceValueOnNextEdit = true;
    }

    public void SetStats(string message)
    {
        _statsText.text = message;
    }

    public void SetFeedback(string message, bool invalid)
    {
        _feedbackText.text = message;
        _feedbackText.color = invalid ? new Color(1f, 0.65f, 0.4f) : new Color(0.55f, 0.87f, 0.81f);
    }

    // 현재 진행 중인 연출/작업 여부에 따라 UI 오브젝트 활성화 관리
    public void SetOperationState(bool busy, bool hasActiveOperation)
    {
        // 연출 중 다른 조작 실행 막기
        _operations.interactable = !busy;
        _operations.blocksRaycasts = !busy;

        // 실행 중인 연출과 작업이 없을 때만 push, pop, 값 입력 활성화
        _pushButton.interactable = !busy && !hasActiveOperation;
        _popButton.interactable = !busy && !hasActiveOperation;
        _valueInput.interactable = !busy && !hasActiveOperation;

        // 실행 중인 연출이 없고, 연출해야 할 push, pop이 있을 경우에만 활성화
        _stepButton.interactable = !busy && hasActiveOperation;
        _playButton.interactable = !busy && hasActiveOperation;
    }

    private void PrepareRetainedValueForEdit(string _)
    {
        if (!_replaceValueOnNextEdit)
            return;

        _valueInput.ActivateInputField();
        _valueInput.selectionAnchorPosition = 0;
        _valueInput.selectionFocusPosition = _valueInput.text.Length;
        _replaceValueOnNextEdit = false;
    }
}
