using Sirenix.OdinInspector;
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>Stack과 Queue에 같은 입력을 적용하고 두 컨테이너의 결과를 View에 전달한다.</summary>
public class StackQueueController : MonoBehaviour
{
    public int Capacity => _view.Capacity;
    public int Count => _stack.Count;
    public bool IsBusy { get; private set; }

    [SerializeField] private StackQueueUIView _view;
    [SerializeField] private StackQueueControls _controls;

    // 두 컨테이너는 같은 삽입을 받지만 삭제 위치가 달라져 LIFO와 FIFO 차이를 표시한다.
    private readonly Stack<object> _stack = new Stack<object>();
    private readonly Queue<object> _queue = new Queue<object>();
    private ValueType _valueType;
    private Tween _activeEffect;

    // 씬 Builder가 연결한 Controls와 빈 두 컨테이너를 시작 전에 준비한다.
    private void Awake()
    {
        _stack.Initialize();
        _queue.Initialize();
        _controls.Connect(this);
        _controls.ShowSetupPanel(true);
    }

    // 선택한 타입으로 입력 규칙을 고정하고 빈 두 컨테이너를 표시한다.
    [Button]
    public void Initialize()
    {
        if (IsBusy)
            return;

        _valueType = _controls.ValueType;
        _stack.Initialize();
        _queue.Initialize();
        _controls.ShowContainers(_valueType);
        RefreshView();
        Report("Ready. Push / Enqueue inserts the same value into both containers.");
    }

    // 같은 값을 Stack의 top과 Queue의 rear에 넣어 이후 삭제 순서의 차이를 만든다.
    [Button]
    public void PushAndEnqueue()
    {
        if (IsBusy)
            return;

        if (Count >= Capacity)
        {
            Report($"Capacity reached. Each container holds up to {Capacity} values.", true);
            return;
        }

        if (!ValueParser.TryParse(_controls.ValueInput, _valueType, out object value))
        {
            Report($"Value: enter a valid {_valueType.ToString().ToLowerInvariant()} (decimal separator: .).", true);
            return;
        }

        _stack.Push(value);
        _queue.Enqueue(value);
        _controls.RetainValueInput();
        RefreshView();
        Report($"Inserted {ValueParser.Format(value)}. Stack top and Queue rear received the value.");
        PlayEffect(_view.AnimateInserted(Count - 1));
    }

    // Stack은 마지막 값, Queue는 첫 값을 동시에 꺼내 같은 입력의 결과 차이를 보여준다.
    [Button]
    public void PopAndDequeue()
    {
        if (IsBusy)
            return;

        if (_stack.IsEmpty())
        {
            Report("Both containers are empty.", true);
            return;
        }

        int stackIndex = Count - 1;
        int queueCount = _queue.Count;
        PlayEffect(_view.AnimateRemoved(stackIndex, queueCount), () =>
        {
            _stack.Pop(out object stackValue);
            _queue.Dequeue(out object queueValue);
            RefreshView();
            Report($"Stack Pop: {ValueParser.Format(stackValue)}    Queue Dequeue: {ValueParser.Format(queueValue)}");
        });
    }

    // 두 View가 각 어댑터의 현재 값만 읽도록 갱신한다.
    private void RefreshView()
    {
        _view.Refresh(_stack, _queue);
        _controls.SetStats($"Count: {Count} / {Capacity}    Type: {_valueType.ToString().ToLowerInvariant()}");
    }

    // 성공과 입력 오류 메시지를 Controls에 전달한다.
    private void Report(string message, bool invalid = false)
    {
        _controls.SetFeedback(message, invalid);
    }

    // 연출 중에는 다음 조작을 잠가 삭제 전 화면과 삭제 후 화면이 섞이지 않게 한다.
    private void PlayEffect(Tween effect, Action completed = null)
    {
        if (effect == null)
        {
            completed?.Invoke();
            return;
        }

        _activeEffect = effect;
        IsBusy = true;
        _controls.SetBusy(true);
        StartCoroutine(FinishEffect(completed));
    }

    // Tween이 끝난 뒤에만 데이터 변경과 슬롯 갱신을 실행한다.
    private IEnumerator FinishEffect(Action completed)
    {
        yield return _activeEffect.WaitForCompletion();

        _activeEffect = null;
        completed?.Invoke();
        IsBusy = false;
        _controls.SetBusy(false);
    }

    // 씬 전환이나 비활성화로 연출이 끊겨도 슬롯의 위치와 입력 잠금을 원래대로 돌린다.
    private void OnDisable()
    {
        StopAllCoroutines();
        if (_activeEffect != null && _activeEffect.IsActive())
            _activeEffect.Kill();

        _activeEffect = null;
        if (_view != null)
            _view.StopEffects();
        IsBusy = false;
        if (_controls != null)
            _controls.SetBusy(false);
    }
}
