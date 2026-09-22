using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 버튼 연결, 키보드 탐색, 타입 선택, 숫자 입력 제한, 연출 중 버튼 잠금
/// </summary>
public class HashTableControls : MonoBehaviour
{
    public ValueType KeyType => (ValueType)_keyTypeDropdown.value;
    public ValueType ValueType => (ValueType)_valueTypeDropdown.value;
    public string KeyInput => _keyInput.text;
    public string ValueInput => _valueInput.text;
    
    // Setup 패널 UI
    [SerializeField] private GameObject _setupPanel;
    [SerializeField] private Dropdown _keyTypeDropdown;
    [SerializeField] private Dropdown _valueTypeDropdown;

    [SerializeField] private CanvasGroup _operations; // 타입 선택 전에는 조작 영역의 입력을 차단한다.

    // 입력 Input Field UI
    [SerializeField] private InputField _keyInput;
    [SerializeField] private InputField _valueInput;

    // 조작 Button UI
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _insertButton;
    [SerializeField] private Button _removeButton;
    [SerializeField] private Button _findButton;
    [SerializeField] private Button _clearButton;

    // 조작 결과 텍스트 UI
    [SerializeField] private Text _typeText;
    [SerializeField] private Text _statsText;
    [SerializeField] private Text _feedbackText;

    private HashTableController _controller;
    private Button[] _actionButtons;
    private Selectable[] _tabOrder;
    private Color[] _normalButtonColors;
    private Color _activeButtonColor;
    private Button _activeButton; // 입력란에 포커스가 돌아가도 Enter로 실행할 조작은 유지한다.
    private EventSystem _navigationOwner;
    private bool _previousNavigationEvents;
    private int _keyboardStartFrame; // Start를 누른 Enter가 같은 프레임에 Insert까지 실행하지 않게 한다.


    // UI 버튼의 클릭 이벤트를 테이블 조작 함수에 연결한다.
    public void Connect(HashTableController owner)
    {
        // 버튼 클릭 시 호출할 HashTableController의 함수들 매핑
        _controller = owner;
        _actionButtons = new[] { _insertButton, _findButton, _removeButton, _clearButton };
        _tabOrder = new Selectable[] { _keyInput, _valueInput, _insertButton, _findButton, _removeButton, _clearButton };
        _activeButtonColor = _insertButton.image.color;
        _normalButtonColors = new Color[_actionButtons.Length];
        for (int i = 0; i < _actionButtons.Length; i++)
            _normalButtonColors[i] = i == 0 ? _findButton.image.color : _actionButtons[i].image.color;

        _startButton.onClick.AddListener(_controller.Initialize);
        _insertButton.onClick.AddListener(Insert);
        _removeButton.onClick.AddListener(Remove);
        _findButton.onClick.AddListener(Find);
        _clearButton.onClick.AddListener(Clear);
    }

    // 이 뷰에서 등록한 리스너만 해제한다.
    private void OnDestroy()
    {
        // 이벤트 해제
        if (_controller == null) return;
        _startButton.onClick.RemoveListener(_controller.Initialize);
        _insertButton.onClick.RemoveListener(Insert);
        _removeButton.onClick.RemoveListener(Remove);
        _findButton.onClick.RemoveListener(Find);
        _clearButton.onClick.RemoveListener(Clear);
    }

    public void ShowSetupPanel(bool show)
    {
        _setupPanel.SetActive(show);
        // 셋업 패널 활성화 전, Operation 패널 입력 막기
        _operations.interactable = !show;
        _operations.blocksRaycasts = !show;
        if (show)
            ReleaseNavigation();
        else
            CaptureNavigation();
    }

    // 선택한 타입을 표시하고 해당 타입의 입력 규칙으로 조작 화면을 준비한다.
    public void StartShowTable(ValueType keyType, ValueType valueType)
    {
        ShowSetupPanel(false);

        _typeText.text = $"KEY / {keyType.ToString().ToLowerInvariant()}     VALUE / {valueType.ToString().ToLowerInvariant()}";
        _keyInput.text = string.Empty;
        _valueInput.text = string.Empty;

        ConfigureInput(_keyInput, keyType);
        ConfigureInput(_valueInput, valueType);

        SelectAction(_insertButton);
        Focus(_keyInput);
    }

    private void OnEnable()
    {
        if (_controller != null && _controller.IsStarted && !_setupPanel.activeSelf)
            CaptureNavigation();
    }

    private void OnDisable()
    {
        ReleaseNavigation();
    }

    // 입력란이 이번 프레임의 문자를 반영한 다음 Tab과 Enter를 처리한다.
    private void LateUpdate()
    {
        if (_controller == null || !_controller.IsStarted || _setupPanel.activeSelf)
            return;

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;
        if (_navigationOwner != eventSystem)
            CaptureNavigation();

        foreach (var button in _actionButtons)
            if (button.gameObject == eventSystem.currentSelectedGameObject && button.IsInteractable())
                SelectAction(button);

        var keyboard = Keyboard.current;
        if (keyboard == null || Time.frameCount == _keyboardStartFrame)
            return;

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            MoveFocus(keyboard.shiftKey.isPressed ? -1 : 1);
            return;
        }

        if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            && !_controller.IsBusy && _activeButton != null
            && _activeButton.IsActive() && _activeButton.IsInteractable())
        {
            _activeButton.onClick.Invoke();
            // Enter로 편집이 종료된 입력란을 다시 활성화해 이어서 입력할 수 있게 한다.
            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.TryGetComponent<InputField>(out var input))
                input.ActivateInputField();
        }
    }

    // 조작 화면에서는 기본 Submit을 차단해 Enter가 버튼을 두 번 실행하지 않게 한다.
    private void CaptureNavigation()
    {
        if (!isActiveAndEnabled || _navigationOwner == EventSystem.current)
            return;
        ReleaseNavigation();
        _navigationOwner = EventSystem.current;
        if (_navigationOwner == null)
            return;
        _previousNavigationEvents = _navigationOwner.sendNavigationEvents;
        // InputField의 문자 입력 이벤트는 이 설정과 무관하게 계속 전달된다.
        _navigationOwner.sendNavigationEvents = false;
        _keyboardStartFrame = Time.frameCount;
    }

    // 설정 화면이나 비활성화 시 기존 UI 탐색 설정을 복원한다.
    private void ReleaseNavigation()
    {
        if (_navigationOwner != null)
            _navigationOwner.sendNavigationEvents = _previousNavigationEvents;
        _navigationOwner = null;
    }

    // 비활성 또는 연출 때문에 잠긴 버튼은 건너뛰고 순환한다.
    private void MoveFocus(int direction)
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        int index = System.Array.FindIndex(_tabOrder, item => item.gameObject == selected);
        if (index < 0)
            index = direction > 0 ? -1 : 0;

        for (int i = 0; i < _tabOrder.Length; i++)
        {
            index = (index + direction + _tabOrder.Length) % _tabOrder.Length;
            var target = _tabOrder[index];
            if (!target.IsActive() || !target.IsInteractable())
                continue;
            Focus(target);
            if (target is Button button)
                SelectAction(button);
            return;
        }
    }

    // 입력란으로 이동하면 텍스트 편집도 함께 시작한다.
    private static void Focus(Selectable target)
    {
        target.Select();
        if (target is InputField input)
            input.ActivateInputField();
    }

    // 포커스와 별도로 마지막 조작 버튼의 강조색을 유지한다.
    private void SelectAction(Button selected)
    {
        _activeButton = selected;
        for (int i = 0; i < _actionButtons.Length; i++)
            _actionButtons[i].image.color = _actionButtons[i] == selected
                ? _activeButtonColor : _normalButtonColors[i];
    }

    // 마우스 클릭과 Enter 모두 같은 경로로 조작 선택을 기록하고 실행한다.
    private void Insert() { SelectAction(_insertButton); _controller.Add(); }
    private void Find() { SelectAction(_findButton); _controller.Find(); }
    private void Remove() { SelectAction(_removeButton); _controller.Remove(); }
    private void Clear() { SelectAction(_clearButton); _controller.Clear(); }

    // Unity의 기본 검증을 사용해 숫자 입력란에서 허용하지 않는 문자를 무시한다.
    private static void ConfigureInput(InputField input, ValueType type)
    {
        input.contentType = type == ValueType.Int ? InputField.ContentType.IntegerNumber
            : type == ValueType.Float ? InputField.ContentType.DecimalNumber
            : InputField.ContentType.Standard;
    }

    // 연출 도중에 들어온 다음 조작을 무시하기 위해.
    public void SetBusy(bool busy)
    {
        _insertButton.interactable = _removeButton.interactable = _findButton.interactable = _clearButton.interactable = !busy;
    }

    public void SetStats(string message)
    {
        _statsText.text = message;
    }

    // 조작 결과를 표시하고 유효하지 않은 입력은 별도 색으로 구분한다.
    public void SetFeedback(string message, bool invalid)
    {
        _feedbackText.text = message;
        _feedbackText.color = invalid ? new Color(1f, 0.65f, 0.4f) : new Color(0.55f, 0.87f, 0.81f);
    }
}
