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
    public void Refresh(Deque<object> deque, 
        int mapIndex, 
        DequeIterator start, 
        DequeIterator finish,
        int changedStart, 
        int changedCount, 
        int selectedIndex)
    {
        bool hasBlock = deque.HasBlock(mapIndex);
        int slotCount = hasBlock ? deque.BlockSize : 0;

        // 해당 map 행에 블록이 할당되어 있는지 퓨시한다.
        _mapText.text = $"[{mapIndex}] -> " + (hasBlock ? "block" : "[] null");

        // map 슬롯 텍스트에 이터레이너 표시
        if (!hasBlock)
        {
            if (start.Node == mapIndex) 
                _mapText.text += $"\nS → {start.Curr}";

            if (finish.Node == mapIndex) 
                _mapText.text += $"\nF → {finish.Curr}";
        }

        // null block은 슬롯 개수가 0이므로 슬롯을 새로 만들지 않는다.
        while (_slots.Count < slotCount)
        {
            var slot = Instantiate(_slotPrefab, _slotRoot);
            slot.name = "Slot " + _slots.Count;
            _slots.Add(slot);
        }

        int startOffset = start.Node * deque.BlockSize + start.Curr;
        for (int i = 0; i < _slots.Count; i++)
        {
            // 남는 슬롯은 Layout Group에서도 빠지도록 비활성화한다.
            _slots[i].gameObject.SetActive(i < slotCount);
            if (i >= slotCount)
                continue;

            // 변경은 물리 슬롯 범위로, 조회는 논리 index로 판정한다. 삭제된 빈 칸도 변경에 포함된다.
            int offset = mapIndex * deque.BlockSize + i;
            int index = offset - startOffset;
            bool occupied = index >= 0 && index < deque.Count;

            // 유효 범위 안의 값만 읽는다. finish나 삭제된 빈 슬롯은 역참조하지 않는다.
            _slots[i].Bind(i, offset, occupied ? index : -1, occupied ? deque[index] : null,
                mapIndex == start.Node && i == start.Curr,
                mapIndex == finish.Node && i == finish.Curr);

            _slots[i].SetHighlight(offset >= changedStart && offset < changedStart + changedCount,
                occupied && selectedIndex >= 0 && index == selectedIndex);
        }
    }

    public DequeSlotUIView FindLogicalSlot(int logicalIndex)
    {
        foreach (var slot in _slots)
            if (slot.gameObject.activeInHierarchy && slot.LogicalIndex == logicalIndex)
                return slot;

        return null;
    }

    public DequeSlotUIView FindPhysicalSlot(int physicalOffset)
    {
        foreach (var slot in _slots)
            if (slot.gameObject.activeInHierarchy && slot.PhysicalOffset == physicalOffset)
                return slot;

        return null;
    }

    public void StopEffects()
    {
        foreach (var slot in _slots)
            if (slot != null)
                slot.StopEffects();
    }

    public IEnumerable<DequeSlotUIView> ActiveSlots()
    {
        foreach (var slot in _slots)
            if (slot.gameObject.activeInHierarchy)
                yield return slot;
    }
}
