using UnityEngine;
using UnityEngine.UI;

/// <summary>한 슬롯의 값, index, iterator 마커와 조회·변경 강조를 표시한다.</summary>
[RequireComponent(typeof(DequeSlotEffects))]
public class DequeSlotUIView : UIEntry<object>
{
    public DequeSlotEffects Effects => GetComponent<DequeSlotEffects>();
    public int LogicalIndex { get; private set; } = -1;
    public int PhysicalOffset { get; private set; } = -1;
    public bool IsOccupied => LogicalIndex >= 0;

    // 물리 slot 번호, 논리 index와 값 표시 UI
    [SerializeField] private Text _indexText;
    [SerializeField] private Text _valueText;

    // 같은 슬롯에 함께 표시될 수 있는 Start / Finish 마커
    [SerializeField] private Text _startText;
    [SerializeField] private Text _finishText;

    // READ / CHANGED 문구와 상태 색상 UI
    [SerializeField] private Text _stateText;
    [SerializeField] private Image _background;
    [SerializeField] private Outline _outline;
    [SerializeField] private Color _occupiedColor = new Color(0.12f, 0.26f, 0.34f);
    [SerializeField] private Color _emptyColor = new Color(0.065f, 0.10f, 0.15f);

    // 이전 연출을 정리하고 Controller가 저장한 실제 값을 표시한다.
    protected override void OnBind(object data)
    {
        Effects.Cancel();
        _valueText.text = ValueParser.Format(data);
    }

    // 별도 표시 데이터 객체를 만들지 않고, 행에서 계산한 슬롯 위치와 마커를 함께 반영한다.
    public void Bind(int slotIndex, int physicalOffset, int logicalIndex, object value, bool isStart, bool isFinish)
    {
        base.Bind(value);

        // 0이나 빈 문자열도 유효 원소이므로 값 자체가 아닌 논리 index로 빈 칸을 판단한다.
        PhysicalOffset = physicalOffset;
        LogicalIndex = logicalIndex;
        _indexText.text = $"slot {slotIndex}" + (IsOccupied ? $" / {logicalIndex}" : "");
        if (!IsOccupied) 
            _valueText.text = "empty";

        // 빈 deque는 같은 슬롯에 S와 F를 동시에 표시한다. finish의 값은 항상 비어 있다.
        _startText.gameObject.SetActive(isStart);
        _finishText.gameObject.SetActive(isFinish);

        _background.color = IsOccupied ? _occupiedColor : _emptyColor;
        SetHighlight(false, false);
    }

    // 조회는 청록색, 변경은 금색 테두리로 구분하고 배경색 pulse를 재생한다.
    public void SetHighlight(bool changed, bool selected)
    {
        _outline.enabled = changed || selected;
        _outline.effectColor = selected
            ? new Color(0.4f, 0.8f, 1f) 
            : new Color(1f, 0.78f, 0.3f);

        _stateText.text = selected ? 
            "READ" :
            changed ? "CHANGED" : "";

        _stateText.color = _outline.effectColor;

        if (changed || selected)
            Effects.Pulse(_background);
    }

    public DG.Tweening.Tween AnimateAppear()
    {
        return Effects.Appear(_background);
    }

    public DG.Tweening.Tween AnimatePulse()
    {
        return Effects.Pulse(_background);
    }

    public DG.Tweening.Tween AnimateDisappear()
    {
        return Effects.Disappear(_background);
    }

    public DG.Tweening.Tween AnimateShift(int direction)
    {
        return Effects.Shift(_background, direction);
    }

    public DG.Tweening.Tween AnimateShift(int direction, int order)
    {
        return Effects.Shift(_background, direction, order);
    }

    public void StopEffects()
    {
        Effects.Cancel();
    }
}
