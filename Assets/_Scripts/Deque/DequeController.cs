using Sirenix.OdinInspector;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deque 데이터를 소유하고 입력 검증 및 snapshot 갱신을 담당한다.
/// </summary>
public class DequeController : MonoBehaviour
{
    public bool IsStarted { get; private set; }
    public bool IsBusy { get; private set; }
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
    private Tween _activeEffect;

    private void Awake()
    {
        _controls.Connect(this);
        _controls.ShowSetupPanel(true);
    }

    // Controls에서 선택한 타입으로 새 세션을 시작하고 빈 Deque 상태를 표시한다.
    // start 버튼 클릭 시 _controls에서 호출된다. 
    [Button]
    public void Initialize()
    {
        if (IsBusy)
            return;

        IsStarted = true;
        _deque.Initialize(
            Mathf.Max(1, _initialMapSize),
            Mathf.Max(1, _blockSize));

        _valueType = _controls.ValueType;
        _controls.StartShowDeque(_valueType);

        RefreshView();

        Report("Ready. Start is inclusive; Finish is exclusive.");
    }

    [Button]
    public void PushFront()
    {
        if (!IsStarted || IsBusy || !ParseValue(_controls.ValueInput, out object value)) 
            return;

        _deque.PushFront(value);

        // 삽입된 직후, 첫 번째 원소를 강조한다.
        RefreshView(0, 1);
        Report($"PushFront({ValueParser.Format(value)})");
        PlayEffect(_dequeView.AnimateInserted(0));
    }

    [Button]
    public void PushBack()
    {
        if (!IsStarted || IsBusy || !ParseValue(_controls.ValueInput, out object value)) 
            return;

        _deque.PushBack(value);

        // 삽입된 직후, 마지막 원소를 강조한다.
        RefreshView(Count - 1, 1);
        Report($"PushBack({ValueParser.Format(value)})");
        PlayEffect(_dequeView.AnimateInserted(Count - 1));
    }

    [Button]
    public void PopFront()
    {
        if (!IsStarted || IsBusy) 
            return;

        int removedOffset = CurrentStartOffset();
        if (!_deque.PopFront(out object value))
        {
            Report("Deque is empty.", true);
            return;
        }

        // 삭제한 슬롯은 갱신된 start 바로 이전이다. 빈 슬롯이어도 강조한다.
        Report($"PopFront -> {ValueParser.Format(value)}");
        PlayEffect(_dequeView.AnimatePhysicalDisappear(removedOffset), true, -1, 1);
    }

    [Button]
    public void PopBack()
    {
        if (!IsStarted || IsBusy) 
            return;

        int removedOffset = CurrentStartOffset() + Count - 1;
        if (!_deque.PopBack(out object value))
        {
            Report("Deque is empty.", true); 
            return;
        }

        // 유효 index는 Count - 1까지이지만, 삭제 강조는 그 다음 빈 칸까지 허용한다.         
        Report($"PopBack -> {ValueParser.Format(value)}");
        PlayEffect(_dequeView.AnimatePhysicalDisappear(removedOffset), true, Count, 1);
    }

    [Button]
    public void Get()
    {
        if (!IsStarted || IsBusy || !ParseIndex(false, true, out int index)) 
            return;

        // READ 상태를 표시(강조)한다.

        RefreshView(selectedIndex: index);
        object value = _deque[index];
        _controls.SetGetAndRemoveAtValue(ValueParser.Format(value));
        Report($"deque[{index}] -> {ValueParser.Format(value)}");
        PlayEffect(_dequeView.AnimateSlotPulse(index));
    }

    [Button]
    public void Set()
    {
        if (!IsStarted 
            || IsBusy
            || !ParseIndex(false, false, out int index)
            || !ParseValue(_controls.SetAndInsertAtValueInput, out object value)) 
            return;

        _deque[index] = value;

        // 해당 원소만 갱신한다.
        RefreshView(index, 1);
        Report($"deque[{index}] = {ValueParser.Format(value)}");
        _controls.ClearGetAndRemoveAtValue();
        PlayEffect(_dequeView.AnimateSlotPulse(index));
    }

    // Cost Demo 삽입을 수행하고 삽입 슬롯부터 밀려난 마지막 슬롯까지 강조한다.
    [Button]
    public void InsertAt()
    {
        if (!IsStarted 
            || IsBusy
            || !ParseIndex(true, false, out int index))
            return;

        if (!_controls.HasSetValueInput)
        {
            _controls.ClearGetAndRemoveAtValue();
            Report($"Value: enter a valid {_valueType.ToString().ToLowerInvariant()} for InsertAt.", true);
            return;
        }

        if (!ParseValue(_controls.SetAndInsertAtValueInput, out object value))
            return;

        // index 0은 PushFront 경로이므로 이동이 없지만, 중간 삽입의 이동 수는 삽입 전 Count로 계산한다.
        int shifted = index == 0 ? 0 : Count - index;
        _deque.InsertAt(index, value);
        _controls.ClearGetAndRemoveAtValue();

        RefreshView(index, shifted + 1);
        Report(shifted == 0 ? $"InsertAt({index}): end operation, no shifts."
            : $"InsertAt({index}): {shifted} elements shifted right (+1), back to front. O(n).");
        PlayEffect(shifted == 0 
            ? _dequeView.AnimateInserted(index) 
            : _dequeView.AnimateLogicalShift(index, shifted + 1, 1));
    }

    // Cost Demo 삭제를 수행하고 당겨진 원소들과 마지막에 비워진 슬롯까지 강조한다.
    [Button]
    public void RemoveAt()
    {
        if (!IsStarted 
            || IsBusy 
            || !ParseIndex(false,  true, out int index)) 
            return;

        // 맨 앞 삭제는 PopFront 경로이며, 중간 삭제는 index 뒤의 원소 수만큼 이동한다.
        int shifted = index == 0 ? 0 : Count - index - 1;
        int removedOffset = CurrentStartOffset() + index;

        _deque.RemoveAt(index, out object value);
        _controls.SetGetAndRemoveAtValue(ValueParser.Format(value));

        Report(shifted == 0 ? $"RemoveAt({index}) -> {ValueParser.Format(value)}: end operation, no shifts."
            : $"RemoveAt({index}) -> {ValueParser.Format(value)}: {shifted} elements shifted left (-1). O(n).");
        if (shifted == 0)
        {
            PlayEffect(_dequeView.AnimatePhysicalDisappear(removedOffset), true, index == 0 ? -1 : index, 1);
            return;
        }

        RefreshView(index, shifted + 1);
        PlayEffect(_dequeView.AnimateLogicalShift(index, shifted + 1, -1));
    }

    // 현재 map/block 크기로 데이터를 초기화하고 이전 조회·변경 강조도 해제한다.
    [Button]
    public void Clear()
    {
        if (!IsStarted || IsBusy) 
            return;

        bool hadItems = Count > 0;
        _deque.Clear();

        Report("Cleared. Start == Finish; blocks reset at the center of the current map.");
        PlayEffect(hadItems ? _dequeView.AnimateClear() : null, true);
    }

    private void RefreshView(int firstIndex = 0, int changedCount = 0, int selectedIndex = -1)
    {
        // 논리 index를 map 전체의 슬롯 번호로 바꾼다. map 확장 후의 새 start를 기준으로 한다.
        int start = CurrentStartOffset();

        _dequeView.Refresh(_deque, start + firstIndex, changedCount, selectedIndex);
        _controls.SetStats($"Count: {Count}    Map: {_deque.MapSize}    Block size: {_deque.BlockSize}    Range: [start, finish)");
    }

    private int CurrentStartOffset()
    {
        var startIterator = _deque.GetStartIterator();
        return startIterator.Node * _deque.BlockSize + startIterator.Curr;
    }

    // 조작 중 입력을 잠그고, 삭제/초기화처럼 이전 화면에서 연출해야 하는 경우 완료 후 갱신한다.
    private void PlayEffect(Tween effect, bool refreshAfter = false, int firstIndex = 0, int changedCount = 0, int selectedIndex = -1)
    {
        if (effect == null)
        {
            if (refreshAfter)
                RefreshView(firstIndex, changedCount, selectedIndex);

            return;
        }

        _activeEffect = effect;
        IsBusy = true;
        _controls.SetBusy(true);
        StartCoroutine(FinishEffect(refreshAfter, firstIndex, changedCount, selectedIndex));
    }

    private IEnumerator FinishEffect(bool refreshAfter, int firstIndex, int changedCount, int selectedIndex)
    {
        yield return _activeEffect.WaitForCompletion();

        _activeEffect = null;
        if (refreshAfter)
            RefreshView(firstIndex, changedCount, selectedIndex);

        IsBusy = false;
        _controls.SetBusy(false);
    }

    // HashTable과 같은 파서를 사용하고 변환 실패는 UI 안내로 전달한다.
    private bool ParseValue(string text, out object value)
    {
        if (ValueParser.TryParse(text, _valueType, out value)) return true;
        Report($"Value: enter a valid {_valueType.ToString().ToLowerInvariant()} (decimal separator: .).", true);
        return false;
    }

    private bool ParseIndex(bool inserting, bool get, out int index)
    {
        int maximum = inserting ? Count : Count - 1;

        string input = get ? _controls.GetAndRemoveAtInput : _controls.SetAndInsertAtInput;

        if (int.TryParse(input, out index)
            && index >= 0
            && index <= maximum) 
            return true;

        Report(maximum < 0 ?
            "Deque is empty. No valid index."
            : $"Index: enter an integer from 0 to {maximum}.",
            true);

        return false;
    }

    private void Report(string message, bool invalid = false)
    {
        _controls.SetFeedback(message, invalid);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (_activeEffect != null && _activeEffect.IsActive())
            _activeEffect.Kill();

        _activeEffect = null;
        if (_dequeView != null)
            _dequeView.StopEffects();

        IsBusy = false;
        if (_controls != null && IsStarted)
            _controls.SetBusy(false);
    }

    private void OnEnable()
    {
        if (IsStarted)
            RefreshView();
    }
}
