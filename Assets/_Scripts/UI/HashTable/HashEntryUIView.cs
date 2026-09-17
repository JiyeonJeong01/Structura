using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(HashEntryEffects))]
public class HashEntryUIView : UIEntry<HashEntry<object, object>>
{
    public HashEntryEffects Effects => GetComponent<HashEntryEffects>();

    [FormerlySerializedAs("keyText"), SerializeField] private Text _keyText;
    [FormerlySerializedAs("valueText"), SerializeField] private Text _valueText;
    [FormerlySerializedAs("nextText"), SerializeField] private Text _nextText;
    [FormerlySerializedAs("background"), SerializeField] private Image _background;
    [FormerlySerializedAs("normalColor"), SerializeField] private Color _normalColor = new Color(0.12f, 0.19f, 0.28f);
    [FormerlySerializedAs("foundColor"), SerializeField] private Color _foundColor = new Color(0.12f, 0.48f, 0.39f);

    // 연결된 데이터의 키, 값, 다음 포인터를 표시하고 이전 강조 상태를 해제한다.
    protected override void OnBind(HashEntry<object, object> data)
    {
        _keyText.text = HashValueParser.Format(data.Key);
        _valueText.text = HashValueParser.Format(data.Value);
        _nextText.text = data.Next == null ? "-> null" : "->";

        SetState(UIEntryState.Normal);
    }

    // 표시 상태를 변경하고 해당 상태의 색을 연출 복귀 기준으로 설정한다.
    public override void SetState(UIEntryState newState)
    {
        base.SetState(newState);
        Effects.ResetVisual(_background, newState == UIEntryState.Found ? _foundColor : _normalColor);
    }
}
