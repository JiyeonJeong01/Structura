using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>한 고정 슬롯의 번호와 점유 여부를 표현한다.</summary>
public class StackQueueSlotUIView : MonoBehaviour
{
    private const float SlotStride = 120f;

    [SerializeField] private TMP_Text _indexText;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private TMP_Text _orderText;
    [SerializeField] private Image _background;
    [SerializeField] private Color _occupiedColor = new Color(0.12f, 0.26f, 0.34f);
    [SerializeField] private Color _emptyColor = new Color(0.065f, 0.10f, 0.15f);

    private Sequence _effect;
    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Vector3 _restingPosition;
    private Vector3 _restingScale;

    // 값이 null이어도 유효 원소일 수 있으므로 occupied 인자로 빈 칸을 분명히 구분한다.
    public void Bind(int index, object value, bool occupied, string order)
    {
        StopEffects();
        _indexText.text = $"[{index}]";
        _valueText.text = occupied ? ValueParser.Format(value) : "empty";
        _orderText.text = occupied ? order : string.Empty;
        _background.color = occupied ? _occupiedColor : _emptyColor;
    }

    // 새 원소가 채워진 슬롯을 작게 시작해 원래 크기로 되돌리며 삽입을 표시한다.
    public Tween AnimateAppear()
    {
        var effect = BeginEffect();
        _rect.localScale = _restingScale * 0.65f;
        _canvasGroup.alpha = 0.25f;
        effect.Append(_rect.DOScale(_restingScale * 1.08f, 0.16f).SetEase(Ease.OutBack));
        effect.Join(_canvasGroup.DOFade(1f, 0.16f));
        effect.Append(_rect.DOScale(_restingScale, 0.18f).SetEase(Ease.OutElastic));
        effect.OnComplete(RestorePose);
        return effect;
    }

    // 삭제될 슬롯을 컨테이너 바깥 방향으로 이동시키면서 흐리게 해 제거를 표현한다.
    public Tween AnimateDisappear(float direction)
    {
        var effect = BeginEffect();
        effect.Append(_background.DOColor(new Color(1f, 0.42f, 0.28f), 0.24f));
        effect.Append(_rect.DOLocalMoveX(_restingPosition.x + direction * 82f, 0.52f).SetEase(Ease.InQuad));
        effect.Join(_rect.DOScale(_restingScale * 0.72f, 0.52f).SetEase(Ease.InQuad));
        effect.Join(_canvasGroup.DOFade(0f, 0.52f));
        return effect;
    }

    // Queue front 삭제 뒤 살아남은 원소가 왼쪽 빈 슬롯으로 당겨지는 이동을 표시한다.
    public Tween AnimateShiftLeft(int order)
    {
        var effect = BeginEffect();
        float delay = (order - 1) * 0.09f;
        effect.AppendInterval(delay);
        effect.Append(_rect.DOLocalMoveX(_restingPosition.x - SlotStride, 0.48f).SetEase(Ease.OutQuad));
        return effect;
    }

    // 새 연출 전 슬롯의 레이아웃 위치와 크기를 기억하고 이전 연출은 먼저 정리한다.
    private Sequence BeginEffect()
    {
        StopEffects();
        _rect = (RectTransform)transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 이전에 생성된 슬롯 프리팹도 효과를 재생할 수 있도록 필요한 투명도 컴포넌트를 보완한다.
        _restingPosition = _rect.localPosition;
        _restingScale = _rect.localScale;
        _effect = DOTween.Sequence().SetUpdate(true);
        return _effect;
    }

    // 레이아웃이 제어하는 기본 위치로 돌아가 다음 Refresh가 어긋나지 않게 한다.
    private void RestorePose()
    {
        if (_rect != null)
        {
            _rect.localPosition = _restingPosition;
            _rect.localScale = _restingScale == Vector3.zero ? Vector3.one : _restingScale;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    // 연출 중단과 슬롯 재사용 때 이동·투명 상태를 원래대로 되돌린다.
    public void StopEffects()
    {
        if (_effect != null && _effect.IsActive())
            _effect.Kill();

        _effect = null;
        RestorePose();
    }

    // 비활성화되면서 남은 Tween이 다음 씬의 슬롯을 건드리지 않게 종료한다.
    private void OnDisable()
    {
        StopEffects();
    }
}
