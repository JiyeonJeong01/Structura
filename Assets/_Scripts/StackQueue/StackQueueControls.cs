using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>타입 선택과 공통 입력·버튼의 UI 상태를 관리한다.</summary>
public class StackQueueControls : MonoBehaviour
{
    public ValueType ValueType => (ValueType)_valueTypeDropdown.value;
    public string ValueInput => _valueInput.text;

    // 시작 전 타입을 선택하는 오버레이 UI
    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private Dropdown _valueTypeDropdown;

    // 두 컨테이너에 공통으로 적용할 조작 UI
    [SerializeField] private CanvasGroup _operations;
    [SerializeField] private InputField _valueInput;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _pushEnqueueButton;
    [SerializeField] private Button _popDequeueButton;

    // 현재 타입, 용량, 결과 안내 UI
    [SerializeField] private Text _typeText;
    [SerializeField] private Text _statsText;
    [SerializeField] private Text _feedbackText;

    private StackQueueController _controller;
    private EventTrigger _valueInputClickTrigger;
    private EventTrigger.Entry _retainedValueClickEntry;
    private bool _replaceValueOnNextEdit;

    // 버튼의 데이터 변경 요청을 Controller로만 전달한다.
    public void Connect(StackQueueController owner)
    {
        _controller = owner;
        _startButton.onClick.AddListener(_controller.Initialize);
        _pushEnqueueButton.onClick.AddListener(_controller.PushAndEnqueue);
        _popDequeueButton.onClick.AddListener(_controller.PopAndDequeue);
        ConnectRetainedValueClick();
    }

    // 이 Controls가 등록한 클릭 리스너만 해제한다.
    private void OnDestroy()
    {
        if (_controller == null)
            return;

        _startButton.onClick.RemoveListener(_controller.Initialize);
        _pushEnqueueButton.onClick.RemoveListener(_controller.PushAndEnqueue);
        _popDequeueButton.onClick.RemoveListener(_controller.PopAndDequeue);
        if (_valueInputClickTrigger != null && _retainedValueClickEntry != null)
            _valueInputClickTrigger.triggers.Remove(_retainedValueClickEntry);
    }

    // 타입 선택이 끝날 때까지 컨테이너 조작 영역의 입력을 막는다.
    public void ShowSetupPanel(bool show)
    {
        _setupPanel.SetActive(show);
        _operations.interactable = !show;
        _operations.blocksRaycasts = !show;
    }

    // 선택한 타입에 맞춰 InputField의 허용 문자와 상단 안내를 갱신한다.
    public void ShowContainers(ValueType type)
    {
        ShowSetupPanel(false);
        _typeText.text = $"VALUE TYPE  /  {type.ToString().ToUpperInvariant()}";
        _valueInput.text = string.Empty;
        _replaceValueOnNextEdit = false;
        _valueInput.contentType = type == ValueType.Int
            ? InputField.ContentType.IntegerNumber
            : type == ValueType.Float
                ? InputField.ContentType.DecimalNumber
                : InputField.ContentType.Standard;
        _valueInput.Select();
        _valueInput.ActivateInputField();
    }

    // 성공과 오류를 색으로 구분해 같은 안내 영역에 표시한다.
    public void SetFeedback(string message, bool invalid)
    {
        _feedbackText.text = message;
        _feedbackText.color = invalid ? new Color(1f, 0.65f, 0.4f) : new Color(0.55f, 0.87f, 0.81f);
    }

    // Controller가 계산한 현재 원소 수와 최대 용량을 표시한다.
    public void SetStats(string message)
    {
        _statsText.text = message;
    }

    // 같은 값을 연속 삽입할 수 있도록 현재 Value는 보존하고 다음 편집에서만 교체하게 표시한다.
    public void RetainValueInput()
    {
        _replaceValueOnNextEdit = true;
    }

    // Legacy InputField에는 onSelect 이벤트가 없으므로 클릭 이벤트를 직접 연결한다.
    private void ConnectRetainedValueClick()
    {
        _valueInputClickTrigger = _valueInput.GetComponent<EventTrigger>();
        if (_valueInputClickTrigger == null)
            _valueInputClickTrigger = _valueInput.gameObject.AddComponent<EventTrigger>();

        _retainedValueClickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        _retainedValueClickEntry.callback.AddListener(PrepareRetainedValueForEdit);
        _valueInputClickTrigger.triggers.Add(_retainedValueClickEntry);
    }

    // 보존된 값을 다시 편집하려고 클릭한 경우 전체 선택해 첫 글자 입력으로 교체되게 한다.
    private void PrepareRetainedValueForEdit(BaseEventData _)
    {
        if (!_replaceValueOnNextEdit)
            return;

        _valueInput.ActivateInputField();
        _valueInput.selectionAnchorPosition = 0;
        _valueInput.selectionFocusPosition = _valueInput.text.Length;
        _replaceValueOnNextEdit = false;
    }

    // 삭제 연출 중에는 두 버튼과 입력란을 같은 CanvasGroup으로 함께 잠근다.
    public void SetBusy(bool busy)
    {
        _operations.interactable = !busy;
        _operations.blocksRaycasts = !busy;
    }
}
