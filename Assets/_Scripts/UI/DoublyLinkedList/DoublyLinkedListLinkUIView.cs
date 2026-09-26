using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UISettings = DoublyLinkedListUISettings;

// 두 방향의 연결을 별도 선과 화살촉으로 표시한다.
public class DoublyLinkedListLinkUIView : MonoBehaviour
{
    [SerializeField] private RectTransform _forward;
    [SerializeField] private RectTransform _backward;
    [SerializeField] private Graphic[] _graphics;
    public float Progress { get; private set; } = 1f;
    private Vector2 _from;
    private Vector2 _to;

    // 이동 중인 노드의 양 끝 좌표에 맞춰 연결 길이를 갱신한다.
    public void Place(Vector2 from, Vector2 to)
    {
        _from = from;
        _to = to;
        Draw();
    }

    // 다음 배치에서도 끊긴 상태를 유지할 수 있도록 길이 비율을 보관한다.
    public void SetProgress(float progress)
    {
        Progress = progress;
        Draw();
    }

    // 선과 끝에 붙은 화살촉을 함께 접거나 펼친다.
    public Tween Animate(float progress)
    {
        return DOTween.To(() => Progress, SetProgress, progress, UISettings.LinkDuration).SetEase(Ease.InOutSine);
    }

    // 선택한 노드 오른쪽의 두 연결을 같은 색으로 강조한다.
    public void Select(bool selected)
    {
        Color color = selected ? UISettings.ActiveColor : UISettings.LinkColor;
        foreach (var graphic in _graphics)
            graphic.color = color;
    }

    // next는 왼쪽에서, prev는 오른쪽에서 자라도록 각각 시작점을 잡는다.
    private void Draw()
    {
        float length = Mathf.Max(0f, _to.x - _from.x) * Progress;
        _forward.anchoredPosition = _from + Vector2.up * UISettings.LinkLaneOffset;
        _backward.anchoredPosition = _to - Vector2.up * UISettings.LinkLaneOffset;
        _forward.sizeDelta = _backward.sizeDelta = new Vector2(length, UISettings.LinkThickness);
        _forward.gameObject.SetActive(Progress > UISettings.HiddenLinkThreshold);
        _backward.gameObject.SetActive(Progress > UISettings.HiddenLinkThreshold);
    }
}
