using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UISettings = DoublyLinkedListUISettings;

// 실제 리스트와 화면의 순서를 맞추고 연출 중 중복 조작을 막는다.
public class DoublyLinkedListController : MonoBehaviour
{
    public const int Capacity = UISettings.Capacity;
    public bool IsStarted { get; private set; }
    public bool IsBusy { get; private set; }
    public int Count => _list.Count;
    public int ActiveIndex { get; private set; } = UISettings.NoSelection;

    [SerializeField] private DoublyLinkedListControls _controls;
    [SerializeField] private DoublyLinkedListUIView _view;

    private readonly DoublyLinkedList<object> _list = new DoublyLinkedList<object>();
    private ValueType _valueType;
    private Tween _effect;

    // Setup을 먼저 보여주고 버튼을 연결한다.
    private void Awake()
    {
        _controls.Connect(this);
        _controls.ShowSetup(true);
    }

    // 선택한 타입으로 빈 리스트를 시작한다.
    public void Initialize()
    {
        if (IsBusy || IsStarted)
            return;

        _list.Initialize();
        _valueType = _controls.ValueType;

        IsStarted = true;

        _controls.Begin(_valueType);
        Refresh();
        _controls.SetFeedback("Ready. Select a node; insert after it or delete it.");
    }

    // first/last/current 버튼을 실제 리스트의 삽입 위치로 바꾼다.
    public void Insert(int location)
    {
        if (!IsStarted || IsBusy)
            return;

        if (Count == Capacity)
        {
            _controls.SetFeedback($"The list is full ({Capacity} nodes).", true);
            return;
        }

        if (!TryReadValue(out object value))
            return;

        int index = location == UISettings.FirstLocation 
            ? 0 /* First */ 
            : location == UISettings.LastLocation 
                ? Count /* Last */
                : ActiveIndex + 1; /* InsertAfter */

        StartCoroutine(InsertRoutine(index, value));
    }

    // 삭제 버튼은 입력값과 무관하게 첫 노드, 끝 노드, 선택 노드를 지운다.
    public void Delete(int location)
    {
        if (!IsStarted || IsBusy || Count == 0)
            return;

        int index = location == UISettings.FirstLocation 
            ? 0 /* First */
            : location == UISettings.LastLocation 
                ? Count - 1 /* Last */ 
                : ActiveIndex; /* Current */

        StartCoroutine(DeleteRoutine(index));
    }

    // 양 끝에서는 멈추며 선택 노드와 오른쪽 연결을 함께 갱신한다.
    public void MoveSelection(int direction)
    {
        if (!IsStarted || IsBusy || Count == 0)
            return;

        // direction은 -1 또는 1이다.
        ActiveIndex = Mathf.Clamp(ActiveIndex + direction, 0, Count - 1);
        Refresh();
    }

    // 탐색값을 한 번 읽어 연출 도중 입력 변경의 영향을 받지 않게 한다.
    public void Find()
    {
        if (!IsStarted || IsBusy)
            return;

        if (!TryReadValue(out object value))
            return;

        StartCoroutine(FindRoutine(value));
    }

    // 삽입 효과 및 애니메이션을 재생한다.
    private IEnumerator InsertRoutine(int index, object value)
    {
        SetBusy(true);

        // 간선 끊기
        yield return Play(_view.CollapseLinks(index, 1));

        // 실제 삽입 진행
        if (index == 0)
            _list.AddFirst(value);
        else if (index == Count)
            _list.AddLast(value);
        else
            _list.InsertAfter(IteratorAt(index - 1), value);

        ActiveIndex = index;

        // 삽입 애니메이션 진행하기 : 노드 삽입 및 간선 연결
        yield return Play(_view.Insert(index, ValueParser.Format(value)));
        yield return Play(_view.ExpandLinks());

        SetBusy(false);
        Refresh();

        _controls.RetainValueInput();
        _controls.SetFeedback($"Inserted {ValueParser.Format(value)} at [{index}].");
    }

    // 삭제 대상의 양쪽 연결과 노드를 없앤 뒤 남은 노드를 당겨 연결한다.
    private IEnumerator DeleteRoutine(int index)
    {

        SetBusy(true);
        _list.TryGetValue(IteratorAt(index), out object value);
        yield return Play(_view.Remove(index));

        _list.Remove(IteratorAt(index));
        ActiveIndex = Count == 0
            ? UISettings.NoSelection 
            : Mathf.Min(index, Count - 1);

        // 삭제된 노드 이동시키며 간선 정리
        yield return Play(_view.CloseGap(index));
        
        // 새로운 연결 진행
        yield return Play(_view.ExpandLinks());

        SetBusy(false);
        Refresh();
        _controls.SetFeedback($"Deleted {ValueParser.Format(value)}.");
    }

    // 인덱스 배열 대신 실제 Next를 따라가므로 표시 순서와 탐색 순서가 같다.
    private IEnumerator FindRoutine(object value)
    {
        SetBusy(true);
        _view.ShowSearch("FIND  /  " + ValueParser.Format(value));

        // _head 부터 펄스 진행
        yield return Play(_view.Pulse(UISettings.NoSelection, false));
        
        var iterator = _list.GetHeadIterator();
        int index = 0;
        bool found = false;


        while (iterator != null)
        {
            _list.TryGetValue(iterator, out object current);

            // 비교 펄스 효과
            yield return Play(_view.Pulse(index, false));

            if (Utils.Compare(current, value) == CompareRes.Equal)
            {
                found = true;
                ActiveIndex = index;

                // 발견 펄스 효과
                yield return Play(_view.Pulse(index, true));
                break;
            }

            _list.TryGetNext(iterator, out iterator);
            index++;
        }

        // 발견하지 못한 경우
        if (!found)
        {
            // _tail 펄스 후 missOverlay 강조
            yield return Play(_view.Pulse(Count, false));
            yield return Play(_view.FlashMiss());
        }

        _view.ShowSearch(null);
        SetBusy(false);
        Refresh();
        _controls.SetFeedback(found ? $"Found at [{index}]." : "Value not found.", !found);
    }

    // 기존 공통 파서를 사용해 타입별 검증 규칙을 유지한다.
    private bool TryReadValue(out object value)
    {
        if (ValueParser.TryParse(_controls.ValueInput, _valueType, out value))
            return true;

        _controls.SetFeedback($"Enter a valid {_valueType.ToString().ToLowerInvariant()}.", true);
        return false;
    }

    // 화면 인덱스를 리스트 소유의 유효한 iterator로 변환한다.
    private DoublyLinkedListIterator<object> IteratorAt(int index)
    {
        var iterator = _list.GetHeadIterator();
        for (int i = 0; i < index; i++)
            _list.TryGetNext(iterator, out iterator);

        return iterator;
    }

    // 재활성화 때도 실제 데이터에서 화면을 복구할 수 있도록 값을 순회한다.
    private void Refresh()
    {
        var values = new List<string>();
        var iterator = _list.GetHeadIterator();

        while (iterator != null)
        {
            _list.TryGetValue(iterator, out object value);
            values.Add(ValueParser.Format(value));
            _list.TryGetNext(iterator, out iterator);
        }

        _view.Refresh(values, ActiveIndex);
        _controls.SetState(IsStarted, IsBusy, Count, ActiveIndex);
    }

    // 코루틴 시작과 동시에 UI를 잠가 같은 프레임의 추가 입력도 차단한다.
    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        _controls.SetState(IsStarted, busy, Count, ActiveIndex);
    }

    // 현재 효과를 기록해 씬을 떠날 때 남아 있는 Tween을 정리한다.
    private IEnumerator Play(Tween effect)
    {
        _effect = effect;
        yield return effect.WaitForCompletion();
        _effect = null;
    }

    // 비활성화가 애니메이션 중간에 발생하면 데이터 상태로 즉시 복구한다.
    private void OnDisable()
    {
        StopAllCoroutines();
        _effect?.Kill();
        _effect = null;

        IsBusy = false;

        if (IsStarted)
        {
            _view.ShowSearch(null);
            Refresh();
        }
    }
}
