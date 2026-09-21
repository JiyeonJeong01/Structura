using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Deque 데이터를 소유하고 입력 검증 및 snapshot 갱신을 담당한다.
/// </summary>
public class DequeController : MonoBehaviour
{
    public bool IsStarted { get; private set; }
    public int Count => _deque.Count;

    // 자료구조 초기 크기 설정
    [SerializeField] private int _initialMapSize = 4;
    [SerializeField] private int _blockSize = 8;

    // 데이터 표시와 조작 입력을 담당하는 UI 참조
    [SerializeField] private DequeUIView _dequeView;
    [SerializeField] private DequeControls _controls;

    // 선택한 타입으로 파싱한 값을 저장한다. Deque 변경은 이 Controller에서만 수행한다.
    private readonly Deque<object> _deque = new Deque<object>();
    private ValueType _valueType;

    // 버튼을 연결하고 데이터 조작 전에 타입 선택 화면부터 표시한다.
    private void Awake()
    {
        _controls.Connect(this);
        _controls.ShowSetupPanel(true);
    }

    // Controls에서 선택한 타입으로 새 세션을 시작하고 빈 Deque 상태를 표시한다.
    // start 버튼 클릭 시 실행된다. 
    [Button]
    public void Initialize()
    {
        _valueType = _controls.ValueType;
        _deque.Initialize(Mathf.Max(1, _initialMapSize), Mathf.Max(1, _blockSize));
        IsStarted = true;
        _controls.StartShowDeque(_valueType);
        RefreshView();
        Report("Ready. Start is inclusive; Finish is exclusive.");
    }

    // 기본 Value 입력을 변환해 맨 앞에 삽입하고 새 첫 원소를 강조한다.
    [Button]
    public void PushFront()
    {
        if (!IsStarted || !ParseValue(_controls.ValueInput, out object value)) return;
        _deque.PushFront(value);
        RefreshView(0, 1);
        Report($"PushFront({HashValueParser.Format(value)})");
    }

    // 맨 뒤에 삽입한 뒤 갱신된 Count를 기준으로 마지막 원소를 강조한다.
    [Button]
    public void PushBack()
    {
        if (!IsStarted || !ParseValue(_controls.ValueInput, out object value)) return;
        _deque.PushBack(value);
        RefreshView(Count - 1, 1);
        Report($"PushBack({HashValueParser.Format(value)})");
    }

    // 첫 원소를 꺼내 결과를 표시한다. 빈 Deque라면 안내만 하고 종료한다.
    [Button]
    public void PopFront()
    {
        if (!IsStarted) return;
        if (!_deque.PopFront(out object value)) { Report("Deque is empty.", true); return; }
        // 삭제한 물리 슬롯은 갱신된 start 바로 이전이다. 빈 슬롯도 강조한다.
        RefreshView(-1, 1);
        Report($"PopFront -> {HashValueParser.Format(value)}");
    }

    // 마지막 원소를 꺼내고 새 finish 위치에 남은 빈 슬롯을 강조한다.
    [Button]
    public void PopBack()
    {
        if (!IsStarted) return;
        if (!_deque.PopBack(out object value)) { Report("Deque is empty.", true); return; }
        // 유효 index는 Count - 1까지이지만, 삭제 강조는 그 다음 빈 칸까지 허용한다.
        RefreshView(Count, 1);
        Report($"PopBack -> {HashValueParser.Format(value)}");
    }

    // 논리 index로 값을 조회하고 변경 강조와 구분되는 READ 상태를 표시한다.
    [Button]
    public void Get()
    {
        if (!IsStarted || !ParseIndex(false, out int index)) return;
        RefreshView(selectedIndex: index);
        Report($"deque[{index}] -> {HashValueParser.Format(_deque[index])}");
    }

    // Index Access의 index와 value를 모두 검증한 뒤 해당 원소만 갱신한다.
    [Button]
    public void Set()
    {
        if (!IsStarted || !ParseIndex(false, out int index)
            || !ParseValue(_controls.IndexValueInput, out object value)) return;
        _deque[index] = value;
        RefreshView(index, 1);
        Report($"deque[{index}] = {HashValueParser.Format(value)}");
    }

    // Cost Demo 삽입을 수행하고 삽입 슬롯부터 밀려난 마지막 슬롯까지 강조한다.
    [Button]
    public void InsertAt()
    {
        if (!IsStarted || !ParseIndex(true, out int index)
            || !ParseValue(_controls.IndexValueInput, out object value)) return;
        // index 0은 PushFront 경로이므로 이동이 없다. 중간 삽입의 이동 수는 삽입 전 Count로 계산한다.
        int shifted = index == 0 ? 0 : Count - index;
        _deque.InsertAt(index, value);
        RefreshView(index, shifted + 1);
        // TODO: snapshot 전후를 이용해 뒤쪽부터 한 칸씩 미는 순차 애니메이션을 추가한다.
        Report(shifted == 0 ? $"InsertAt({index}): end operation, no shifts."
            : $"InsertAt({index}): {shifted} elements shifted right (+1), back to front. O(n).");
    }

    // Cost Demo 삭제를 수행하고 당겨진 원소들과 마지막에 비워진 슬롯까지 강조한다.
    [Button]
    public void RemoveAt()
    {
        if (!IsStarted || !ParseIndex(false, out int index)) return;
        // 맨 앞 삭제는 PopFront 경로이며, 중간 삭제는 index 뒤의 원소 수만큼 이동한다.
        int shifted = index == 0 ? 0 : Count - index - 1;
        _deque.RemoveAt(index, out object value);
        RefreshView(index == 0 ? -1 : index, shifted + 1);
        // TODO: snapshot 전후를 이용해 뒤쪽 원소를 한 칸씩 당기는 순차 애니메이션을 추가한다.
        Report(shifted == 0 ? $"RemoveAt({index}) -> {HashValueParser.Format(value)}: end operation, no shifts."
            : $"RemoveAt({index}) -> {HashValueParser.Format(value)}: {shifted} elements shifted left (-1). O(n).");
    }

    // 현재 map/block 크기로 데이터를 초기화하고 이전 조회·변경 강조도 해제한다.
    [Button]
    public void Clear()
    {
        if (!IsStarted) return;
        _deque.Clear();
        RefreshView();
        Report("Cleared. Start == Finish; blocks reset at the center of the current map.");
    }

    // UI에는 값과 위치의 복사본만 전달한다. changedCount는 삭제 후 빈 슬롯도 포함한다.
    private void RefreshView(int firstIndex = 0, int changedCount = 0, int selectedIndex = -1)
    {
        var snapshot = _deque.GetSnapshot();
        // 논리 index를 map 전체의 물리 슬롯 번호로 바꾼다. map 확장 후의 새 start를 기준으로 한다.
        int start = snapshot.Start.Node * snapshot.BlockSize + snapshot.Start.Curr;
        _dequeView.Refresh(snapshot, start + firstIndex, changedCount, selectedIndex);
        _controls.SetStats($"Count: {Count}    Map: {snapshot.MapSize}    Block size: {snapshot.BlockSize}    Range: [start, finish)");
    }

    // HashTable과 같은 파서를 사용하고 변환 실패는 UI 안내로 전달한다.
    private bool ParseValue(string text, out object value)
    {
        if (HashValueParser.TryParse(text, _valueType, out value)) return true;
        Report($"Value: enter a valid {_valueType.ToString().ToLowerInvariant()} (decimal separator: .).", true);
        return false;
    }

    // 삽입만 index == Count를 허용한다. 조회·갱신·삭제는 실제 원소 범위 안에서만 수행한다.
    private bool ParseIndex(bool inserting, out int index)
    {
        int maximum = inserting ? Count : Count - 1;
        if (int.TryParse(_controls.IndexInput, out index) && index >= 0 && index <= maximum) return true;
        Report(maximum < 0 ? "Deque is empty. No valid index."
            : $"Index: enter an integer from 0 to {maximum}.", true);
        return false;
    }

    // 조작 결과와 입력 오류를 Controls의 피드백 텍스트에 전달한다.
    private void Report(string message, bool invalid = false)
    {
        _controls.SetFeedback(message, invalid);
    }
}
