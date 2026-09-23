using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>Heap 데이터를 소유하고 Step 단위 조작 상태를 관리한다.</summary>
public class HeapController : MonoBehaviour
{
    private enum HeapOperation { None, Push, Pop }

    public bool IsStarted { get; private set; }
    public bool IsBusy { get; private set; }
    public int Count => _heap.Count;

    [SerializeField] private HeapUIView _heapView;
    [SerializeField] private HeapControls _controls;

    private readonly Heap<object> _heap = new Heap<object>();
    private ValueType _valueType;
    private HeapOperation _activeOperation;
    private int _currentIndex = -1;
    private Tween _activeEffect;
    private Coroutine _playRoutine;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("Play가 연속 Step을 실행할 때 각 Step 애니메이션 사이에 쉬는 시간입니다.")]
    private float _playStepInterval = 0.12f;

    private void Awake()
    {
        _controls.Connect(this);
        _controls.ShowSetupPanel(true);
        _controls.SetOperationState(false, false);
    }

    [Button]
    public void Initialize()
    {
        if (IsBusy)
            return;

        IsStarted = true;

        _heap.Initialize();
        _valueType = _controls.ValueType;
        _activeOperation = HeapOperation.None;
        _currentIndex = -1;
        _controls.StartShowHeap(_valueType);

        RefreshView();
        Report("Ready. Push adds a value first; Step or Play restores max heap order.");
    }

    [Button]
    public void Push()
    {
        if (!CanStartOperation())
            return;

        if (Count >= _heapView.Capacity)
        {
            Report($"Heap is full. Capacity is {_heapView.Capacity}.", true);
            return;
        }

        if (!ValueParser.TryParse(_controls.ValueInput, _valueType, out object value))
        {
            Report($"Value: enter a valid {_valueType.ToString().ToLowerInvariant()} (decimal separator: .).", true);
            return;
        }

        // 맨 마지막에 원소 삽입
        _currentIndex = _heap.BeginPushStep(value);
        _activeOperation = HeapOperation.Push;
        _controls.RetainValueInput();

        RefreshView(_currentIndex);
        Report($"Inserted {ValueParser.Format(value)} at index {_currentIndex}. Press Step or Play to sift up.");

        PlayEffect(_heapView.AnimateAppear(_currentIndex),
            completeIfSettled: true,
            completedMessage: "Heap property satisfied / operation complete.",
            pendingMessage: $"Inserted {ValueParser.Format(value)} at index {_currentIndex}. Press Step or Play to sift up.");
    }

    [Button]
    public void Pop()
    {
        if (!CanStartOperation())
            return;

        if (_heap.IsEmpty())
        {
            Report("Heap is empty.", true);
            return;
        }

        int lastIndex = Count - 1;
        // 마지막 원소 루트 위치로 이동
        int nextIndex = _heap.BeginPopStep(out object value);
        Report($"Pop -> {ValueParser.Format(value)}");

        if (nextIndex < 0) // Wrong Index
        {
            _activeOperation = HeapOperation.None;
            _currentIndex = -1;
            
            PlayEffect(_heapView.AnimatePopMove(lastIndex), true, "Single value removed. Heap is empty.");
            return;
        }

        _activeOperation = HeapOperation.Pop;
        _currentIndex = nextIndex;
        PlayEffect(_heapView.AnimatePopMove(lastIndex), true,
            completedMessage: "Heap property satisfied / operation complete.",
            pendingMessage: "Last value moved to root. Press Step or Play to sift down.",
            completeIfSettled: true);
    }

    [Button]
    public void Step()
    {
        if (!IsStarted || IsBusy || _activeOperation == HeapOperation.None)
        {
            Report("No active heap operation. Push or Pop first.", true);
            return;
        }

        StartCoroutine(RunStep());
    }

    [Button]
    public void Play()
    {
        if (!IsStarted || IsBusy || _activeOperation == HeapOperation.None)
        {
            Report("No active heap operation. Push or Pop first.", true);
            return;
        }

        _playRoutine = StartCoroutine(RunPlay());
    }

    public void Clear()
    {
        if (!IsStarted)
            return;

        if (IsBusy || _activeOperation != HeapOperation.None)
        {
            Report("Finish the active operation.", true);
            return;
        }

        _heap.Clear();
        _activeOperation = HeapOperation.None;
        _currentIndex = -1;

        RefreshView();
        Report("Heap cleared.");
    }

    private bool CanStartOperation()
    {
        if (!IsStarted || IsBusy)
            return false;

        if (_activeOperation == HeapOperation.None)
            return true;

        Report("Finish the active operation with Step or Play first.", true);
        return false;
    }

    private IEnumerator RunPlay()
    {
        while (_activeOperation != HeapOperation.None)
        {
            // Run Step 1회 호출
            yield return RunStep();

            // activeOperation이라면 0.12초 쉰 뒤, 다음 step 진행
            if (_activeOperation != HeapOperation.None)
                yield return new WaitForSecondsRealtime(_playStepInterval);
        }

        _playRoutine = null;
    }

    private IEnumerator RunStep()
    {
        IsBusy = true;
        _controls.SetOperationState(true, true);

        int fromIndex = _currentIndex;
        int nextIndex = _activeOperation == HeapOperation.Push
            ? _heap.SiftUpStep(fromIndex)
            : _heap.SiftDownStep(fromIndex);
        // 이번 step에서 비교한 인덱스
        int compareIndex = _heap.LastStepCompareIndex;

        bool siftCompleted = nextIndex < 0;

        _heapView.SetHighlights(fromIndex, compareIndex);
        _activeEffect = siftCompleted
            ? _heapView.AnimatePairPulse(fromIndex, compareIndex) // sift 목표 달성 후 중지(강조 효과)
            : _heapView.AnimateSwap(fromIndex, nextIndex);                  // swap 하며 sift 진행 

        // 애니메이션 끝날 때까지 대기
        if (_activeEffect != null)
            yield return _activeEffect.WaitForCompletion();

        _activeEffect = null;

        if (siftCompleted)
        {
            _activeOperation = HeapOperation.None;
            _currentIndex = -1;
            RefreshView();
            Report("Heap property satisfied / operation complete.");
        }
        else
        {
            _currentIndex = nextIndex;
            RefreshView(_currentIndex);
            // 이미 목표 위치로 정착했다면 다음 턴에 step을 누르지 않고, 바로 강조 후 종료되게 한다. 
            yield return CompleteIfCurrentSettled(
                "Heap property satisfied / operation complete.",
                $"Swapped indices {fromIndex} and {nextIndex}. Step again or press Play.");
        }

        IsBusy = false;
        _controls.SetOperationState(false, _activeOperation != HeapOperation.None);
    }

    private void RefreshView(int currentIndex = -1, int compareIndex = -1)
    {
        _heapView.Refresh(_heap, currentIndex, compareIndex);
        _controls.SetStats($"Count: {Count} / {_heapView.Capacity}    Type: {_valueType.ToString().ToLowerInvariant()}");
        _controls.SetOperationState(IsBusy, _activeOperation != HeapOperation.None);
    }

    private void PlayEffect(Tween effect, bool refreshAfter = false,
        string completedMessage = null, string pendingMessage = null, bool completeIfSettled = false)
    {
        if (effect == null)
        {
            if (refreshAfter)
                RefreshView(_currentIndex);

            if (completeIfSettled)
            {
                StartCoroutine(CompleteIfCurrentSettled(completedMessage, pendingMessage));
                return;
            }

            if (!string.IsNullOrEmpty(completedMessage))
                Report(completedMessage);
            return;
        }

        _activeEffect = effect;
        IsBusy = true;
        _controls.SetOperationState(true, _activeOperation != HeapOperation.None);
        StartCoroutine(FinishEffect(refreshAfter, completedMessage, pendingMessage, completeIfSettled));
    }

    private IEnumerator FinishEffect(bool refreshAfter, string completedMessage, string pendingMessage, bool completeIfSettled)
    {
        yield return _activeEffect.WaitForCompletion();

        _activeEffect = null;
        if (refreshAfter)
            RefreshView(_currentIndex);

        if (completeIfSettled)
            yield return CompleteIfCurrentSettled(completedMessage, pendingMessage);
        else if (!string.IsNullOrEmpty(completedMessage))
            Report(completedMessage);

        IsBusy = false;
        _controls.SetOperationState(false, _activeOperation != HeapOperation.None);
    }

    // 현재 위치가 목표 위치라면 이동 후 강조 애니메이션 연속 재생
    private IEnumerator CompleteIfCurrentSettled(string completedMessage, string pendingMessage)
    {
        if (_activeOperation == HeapOperation.None)
            yield break;

        int currentIndex = _currentIndex;
        int nextIndex = _activeOperation == HeapOperation.Push
            ? _heap.SiftUpStep(currentIndex, false)
            : _heap.SiftDownStep(currentIndex, false);
        int compareIndex = _heap.LastStepCompareIndex;

        if (nextIndex >= 0)
        {
            if (!string.IsNullOrEmpty(pendingMessage))
                Report(pendingMessage);
            yield break;
        }

        _heapView.SetHighlights(currentIndex, compareIndex);
        _activeEffect = _heapView.AnimatePairPulse(currentIndex, compareIndex);
        if (_activeEffect != null)
            yield return _activeEffect.WaitForCompletion();
        _activeEffect = null;

        _activeOperation = HeapOperation.None;
        _currentIndex = -1;
        RefreshView();
        Report(string.IsNullOrEmpty(completedMessage)
            ? "Heap property satisfied / operation complete."
            : completedMessage);
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
        _playRoutine = null;
        IsBusy = false;

        if (_heapView != null)
            _heapView.StopEffects();
        if (_controls != null && IsStarted)
            _controls.SetOperationState(false, _activeOperation != HeapOperation.None);
    }
}
