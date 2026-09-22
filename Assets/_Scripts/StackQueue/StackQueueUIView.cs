using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>고정 슬롯에 Stack과 Queue의 현재 원소, 접근 방향, 끝 위치를 표시한다.</summary>
public class StackQueueUIView : MonoBehaviour
{
    public int Capacity => _stackSlots.Length;

    // Stack과 Queue에 각각 배치된 고정 길이 슬롯과 끝 위치 라벨
    [SerializeField] private StackQueueSlotUIView[] _stackSlots;
    [SerializeField] private StackQueueSlotUIView[] _queueSlots;
    [SerializeField] private Text _stackTopText;
    [SerializeField] private Text _queueFrontText;
    [SerializeField] private Text _queueRearText;

    // Stack의 바닥은 왼쪽에 유지하고 top만 오른쪽으로 늘어난다.
    public void Refresh(Stack<object> stack, Queue<object> queue)
    {
        RefreshSlots(_stackSlots, stack.Count, stack.GetValueAt, "LIFO");
        RefreshSlots(_queueSlots, queue.Count, queue.GetValueAt, "FIFO");

        _stackTopText.text = stack.TryGetTop(out object top) ? $"TOP\n{ValueParser.Format(top)}" : "TOP\nempty";
        _queueFrontText.text = queue.TryGetFront(out object front) ? $"FRONT\n{ValueParser.Format(front)}" : "FRONT\nempty";
        _queueRearText.text = queue.IsEmpty() ? "REAR\nempty" : $"REAR\n{ValueParser.Format(queue.GetValueAt(queue.Count - 1))}";
    }

    // 삽입 뒤 새로 채워진 오른쪽 끝 슬롯 두 개를 동시에 나타낸다.
    public Tween AnimateInserted(int index)
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(_stackSlots[index].AnimateAppear());
        sequence.Join(_queueSlots[index].AnimateAppear());
        return sequence;
    }

    // 삭제 전 Stack top은 사라지고 Queue front는 사라지며 나머지 Queue 슬롯은 왼쪽으로 이동한다.
    public Tween AnimateRemoved(int stackIndex, int queueCount)
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(_stackSlots[stackIndex].AnimateDisappear(1f));
        sequence.Join(_queueSlots[0].AnimateDisappear(-1f));

        // Queue의 front 뒤 원소만 한 슬롯 간격만큼 움직여 FIFO 삭제의 당김을 보여준다.
        for (int i = 1; i < queueCount; i++)
            sequence.Join(_queueSlots[i].AnimateShiftLeft(i));

        return sequence;
    }

    // 진행 중인 슬롯 Tween을 종료해 새로 갱신되는 슬롯에 이전 위치가 남지 않게 한다.
    public void StopEffects()
    {
        foreach (var slot in _stackSlots)
            slot.StopEffects();

        foreach (var slot in _queueSlots)
            slot.StopEffects();
    }

    // 어댑터가 보관한 순서를 직접 읽어 슬롯을 채우며 빈 슬롯은 value가 아니라 index로 판별한다.
    private static void RefreshSlots(StackQueueSlotUIView[] slots, int count,
        System.Func<int, object> getValue, string order)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool occupied = i < count;
            object value = occupied ? getValue(i) : null;
            slots[i].Bind(i, value, occupied, order);
        }
    }
}
