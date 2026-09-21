using UnityEngine;
using UnityEngine.UI;

/// <summary>한 슬롯의 값, index, iterator 마커와 조회·변경 강조를 표시한다.</summary>
[RequireComponent(typeof(DequeSlotEffects))]
public class DequeSlotUIView : UIEntry<Deque<object>.SlotSnapshot>
{
    public DequeSlotEffects Effects => GetComponent<DequeSlotEffects>();

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

    // 재사용 중인 슬롯의 이전 연출과 강조를 정리하고 새 snapshot 값으로 갱신한다.
    protected override void OnBind(Deque<object>.SlotSnapshot data)
    {
        Effects.Cancel();
        _indexText.text = $"slot {data.SlotIndex}" + (data.IsOccupied ? $" / i:{data.LogicalIndex}" : "");
        // 0이나 빈 문자열도 유효 원소이므로 값 자체가 아닌 IsOccupied로 빈 칸을 판단한다.
        _valueText.text = data.IsOccupied ? HashValueParser.Format(data.Value) : "empty";
        // 빈 deque는 같은 슬롯에 S와 F를 동시에 표시한다. finish의 값은 항상 비어 있다.
        _startText.gameObject.SetActive(data.IsStart);
        _finishText.gameObject.SetActive(data.IsFinish);
        _background.color = data.IsOccupied ? _occupiedColor : _emptyColor;
        SetHighlight(false, false);
    }

    // 조회는 청록색, 변경은 금색 테두리로 구분하고 배경색 pulse를 재생한다.
    public void SetHighlight(bool changed, bool selected)
    {
        _outline.enabled = changed || selected;
        _outline.effectColor = selected ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.78f, 0.3f);
        _stateText.text = selected ? "READ" : changed ? "CHANGED" : "";
        _stateText.color = _outline.effectColor;
        if (changed || selected) Effects.Pulse(_background);
    }
}
