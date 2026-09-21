using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>map 한 행의 block 참조와 가로 슬롯 배열을 표시한다.</summary>
public class DequeMapRowUIView : MonoBehaviour
{
    // map 참조 텍스트와 block 슬롯을 정렬할 부모 및 프리팹
    [SerializeField] private Text _mapText;
    [SerializeField] private RectTransform _slotRoot;
    [SerializeField] private DequeSlotUIView _slotPrefab;

    // 할당이 해제된 block의 슬롯 UI도 보관하여 다음 Refresh에서 재사용한다.
    private readonly List<DequeSlotUIView> _slots = new List<DequeSlotUIView>();

    // block 할당 상태에 맞춰 슬롯을 확보하고 변경 범위와 조회 index를 각 슬롯에 전달한다.
    public void Refresh(Deque<object>.MapRowSnapshot row, Deque<object>.Snapshot snapshot,
        int changedStart, int changedCount, int selectedIndex)
    {
        // null 참조와 할당됐지만 비어 있는 블록을 명확히 구분한다.
        _mapText.text = $"[{row.MapIndex}] -> " + (row.HasBlock ? "block" : "[] null");
        if (!row.HasBlock)
        {
            // 슬롯이 없는 행은 iterator 위치를 map 참조 텍스트에 표시한다.
            if (snapshot.Start.Node == row.MapIndex) _mapText.text += $"\nS @ {snapshot.Start.Curr}";
            if (snapshot.Finish.Node == row.MapIndex) _mapText.text += $"\nF @ {snapshot.Finish.Curr}";
        }
        // null block은 Slots가 비어 있으므로 슬롯을 새로 만들지 않는다.
        while (_slots.Count < row.Slots.Count)
        {
            var slot = Instantiate(_slotPrefab, _slotRoot);
            slot.name = "Slot " + _slots.Count;
            _slots.Add(slot);
        }
        for (int i = 0; i < _slots.Count; i++)
        {
            // 남는 슬롯은 Layout Group에서도 빠지도록 비활성화한다.
            _slots[i].gameObject.SetActive(i < row.Slots.Count);
            if (i >= row.Slots.Count) continue;
            // 변경은 물리 슬롯 범위로, 조회는 논리 index로 판정한다. 삭제된 빈 칸도 변경에 포함된다.
            int offset = row.MapIndex * snapshot.BlockSize + i;
            _slots[i].Bind(row.Slots[i]);
            _slots[i].SetHighlight(offset >= changedStart && offset < changedStart + changedCount,
                selectedIndex >= 0 && row.Slots[i].LogicalIndex == selectedIndex);
        }
    }
}
