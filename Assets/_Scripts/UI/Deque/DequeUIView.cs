using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deque를 조회하여 전체 MAP과 Start/Finish 카드를 표시한다.
/// </summary>
public class DequeUIView : MonoBehaviour
{
    // 세로 map 목록을 구성할 부모와 행 프리팹
    [SerializeField] private RectTransform _mapRoot;
    [SerializeField] private DequeMapRowUIView _rowPrefab;

    // MAP 왼쪽의 iterator 상세 정보 UI
    [SerializeField] private Text _startText;
    [SerializeField] private Text _finishText;

    // 리스트 index를 실제 map index와 일치시켜 행을 재사용한다.
    private readonly List<DequeMapRowUIView> _rows = new List<DequeMapRowUIView>();

    // Deque 변경은 Controller에서 수행하고, View는 전달받은 Deque를 읽어 표시한다.
    public void Refresh(Deque<object> deque, int changedStart = 0, int changedCount = 0, int selectedIndex = -1)
    {
        var start = deque.GetStartIterator();
        var finish = deque.GetFinishIterator();

        // map이 확장된 만큼만 행을 추가한다.
        while (_rows.Count < deque.MapSize)
        {
            var row = Instantiate(_rowPrefab, _mapRoot);
            row.name = "Map " + _rows.Count;
            _rows.Add(row);
        }

        // 새 세션에서 map이 작아지면 남는 행은 숨기고 다음 확장 때 재사용한다.
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].gameObject.SetActive(i < deque.MapSize);

            if (i < deque.MapSize)
                _rows[i].Refresh(deque, i, start, finish, changedStart, changedCount, selectedIndex);
        }

        _startText.text = IteratorText(deque, start, false);
        _finishText.text = IteratorText(deque, finish, true);
    }

    // iterator의 네 필드와 실제 접근 위치를 표시한다. finish와 빈 상태의 start는 역참조하지 않는다.
    private static string IteratorText(Deque<object> deque, DequeIterator iterator, bool finish)
    {
        string value = finish 
            ? "past-the-end (no value)" : 
            deque.Count == 0 
                ? "empty (no value)" : ValueParser.Format(deque[0]);

        return $"Node   {iterator.Node}\nCurr    {iterator.Curr}\nFirst    {iterator.First}\nLast    {iterator.Last} (exclusive)\n\nmap[{iterator.Node}][{iterator.Curr}]\n{value}";
    }

    public Tween AnimateInserted(int logicalIndex)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_mapRoot);

        var slot = FindLogicalSlot(logicalIndex);
        return slot == null ? null : slot.AnimateAppear();
    }

    public Tween AnimateSlotPulse(int logicalIndex)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_mapRoot);

        var slot = FindLogicalSlot(logicalIndex);
        return slot == null ? null : slot.AnimatePulse();
    }

    public Tween AnimatePhysicalDisappear(int physicalOffset)
    {
        var slot = FindPhysicalSlot(physicalOffset);
        return slot == null ? null : slot.AnimateDisappear();
    }

    public Tween AnimateLogicalShift(int firstIndex, int count, int direction)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_mapRoot);

        Sequence sequence = null;
        bool movesRight = direction > 0;
        for (int step = 0; step < count; step++)
        {
            int indexOffset = movesRight ? count - 1 - step : step;
            var slot = FindLogicalSlot(firstIndex + indexOffset);
            if (slot == null)
                continue;

            if (sequence == null)
                sequence = DOTween.Sequence().SetUpdate(true);

            sequence.Insert(step * 0.075f, slot.AnimateShift(direction, step));
        }

        return sequence;
    }

    public Tween AnimateClear()
    {
        Sequence sequence = null;
        foreach (var row in _rows)
        {
            if (!row.gameObject.activeInHierarchy)
                continue;

            foreach (var slot in row.ActiveSlots())
            {
                if (!slot.IsOccupied)
                    continue;

                if (sequence == null)
                    sequence = DOTween.Sequence().SetUpdate(true);

                sequence.Join(slot.AnimateDisappear());
            }
        }

        return sequence;
    }

    public void StopEffects()
    {
        foreach (var row in _rows)
            if (row != null)
                row.StopEffects();
    }

    private DequeSlotUIView FindLogicalSlot(int logicalIndex)
    {
        foreach (var row in _rows)
        {
            var slot = row.FindLogicalSlot(logicalIndex);
            if (slot != null)
                return slot;
        }

        return null;
    }

    private DequeSlotUIView FindPhysicalSlot(int physicalOffset)
    {
        foreach (var row in _rows)
        {
            var slot = row.FindPhysicalSlot(physicalOffset);
            if (slot != null)
                return slot;
        }

        return null;
    }
}
