using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UISettings = DoublyLinkedListUISettings;

// 세 칸 노드의 값과 선택/탐색 외곽선을 표현한다.
public class DoublyLinkedListNodeUIView : MonoBehaviour
{
    public RectTransform Rect => (RectTransform)transform;
    public CanvasGroup Group => GetComponent<CanvasGroup>();
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private TMP_Text _leftText;
    [SerializeField] private TMP_Text _rightText;
    [SerializeField] private Outline _outline;

    // 센티널의 바깥 포인터는 null이며 일반 노드는 양쪽 포인터를 갖는다.
    public void SetValue(string value, bool head = false, bool tail = false)
    {
        _valueText.text = value;
        _leftText.text = head ? "null" : "prev";
        _rightText.text = tail ? "null" : "next";
    }

    // 평상시 선택 외곽선으로 되돌린다.
    public void Select(bool selected)
    {
        _outline.enabled = selected;
        _outline.effectColor = UISettings.ActiveColor;

        // 두께
        _outline.effectDistance = new Vector2(UISettings.SelectedOutlineWidth, -UISettings.SelectedOutlineWidth);
    }

    // 순회는 노란색, 일치는 초록색 외곽선을 두껍게 드러낸다.
    public Tween Pulse(bool found)
    {
        _outline.enabled = true;
        _outline.effectColor = found ? UISettings.FoundColor : UISettings.SearchColor;

        return DOTween.To(() => UISettings.PulseStartWidth,
            width => _outline.effectDistance = new Vector2(width, -width),
            UISettings.PulsePeakWidth, found ? UISettings.FoundPulseDuration : UISettings.SearchPulseDuration)
            .SetLoops(UISettings.PulseLoopCount, LoopType.Yoyo);
    }
}
