using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>슬롯의 배경색 강조와 연출 중단 시 원상 복구를 담당한다.</summary>
[RequireComponent(typeof(CanvasGroup))]
public class DequeSlotEffects : MonoBehaviour
{
    [SerializeField] private float _pulseDuration = 0.35f;
    [SerializeField] private float _motionDuration = 0.34f;

    // 연속 조작 시 기존 Tween을 중단하고 당시의 기본 배경색으로 되돌리기 위한 상태
    private Sequence _motion;
    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Image _background;
    private Color _restingColor;
    private Vector3 _restingScale;
    private Quaternion _restingRotation;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CapturePose();
    }

    // 데이터 변경은 즉시 완료한다. 색상만 연출하므로 입력 잠금이나 완료 대기가 필요 없다.
    public Tween Pulse(Image background)
    {
        var sequence = Begin(background);
        background.color = Color.Lerp(_restingColor, Color.white, 0.55f);
        // Time.timeScale과 무관하게 UI 강조가 끝나도록 unscaled 시간으로 재생한다.
        sequence.Append(background.DOColor(_restingColor, _pulseDuration));
        sequence.Join(_rect.DOScale(_restingScale * 1.04f, _pulseDuration * 0.45f).SetEase(Ease.OutQuad));
        sequence.Append(_rect.DOScale(_restingScale, _pulseDuration * 0.55f).SetEase(Ease.OutElastic));
        sequence.OnComplete(RestorePose);
        return sequence;
    }

    // 새 값이 들어온 슬롯을 짧게 확대하고 밝히며 삽입을 표시한다.
    public Tween Appear(Image background)
    {
        var sequence = Begin(background);
        _rect.localScale = _restingScale * 0.72f;
        _canvasGroup.alpha = 0.35f;
        background.color = Color.Lerp(_restingColor, Color.white, 0.8f);
        sequence.Append(_rect.DOScale(_restingScale * 1.08f, 0.14f).SetEase(Ease.OutBack));
        sequence.Join(_canvasGroup.DOFade(1f, 0.14f));
        sequence.Append(_rect.DOScale(_restingScale, 0.2f).SetEase(Ease.OutElastic));
        sequence.Join(background.DOColor(_restingColor, 0.2f));
        sequence.OnComplete(RestorePose);
        return sequence;
    }

    // 삭제 대상 슬롯을 붉게 밝힌 뒤 작아지며 사라지게 한다. 실제 UI 갱신은 Controller가 완료 후 수행한다.
    public Tween Disappear(Image background)
    {
        var sequence = Begin(background);
        sequence.Append(_rect.DOLocalRotate(new Vector3(0f, 0f, -4f), 0.08f));
        sequence.Join(background.DOColor(new Color(1f, 0.42f, 0.28f), 0.08f));
        sequence.Append(_rect.DOLocalRotate(new Vector3(0f, 0f, 5f), 0.08f));
        sequence.Append(_rect.DOScale(0.18f, 0.18f).SetEase(Ease.InBack));
        sequence.Join(_canvasGroup.DOFade(0f, 0.16f));
        return sequence;
    }

    // 중간 삽입/삭제에서 한 칸 밀리거나 당겨지는 비용을 방향성 있는 흔들림으로 보여준다.
    public Tween Shift(Image background, int direction)
    {
        return Shift(background, direction, 0);
    }

    // order가 커질수록 조금 더 강하게 반응해 여러 슬롯이 순서대로 밀리는 느낌을 만든다.
    public Tween Shift(Image background, int direction, int order)
    {
        var sequence = Begin(background);
        float signed = Mathf.Sign(direction == 0 ? 1 : direction);
        float strength = Mathf.Clamp01(order / 6f);
        float angle = Mathf.Lerp(4f, 8f, strength);
        float widthScale = Mathf.Lerp(1.08f, 1.16f, strength);
        float heightScale = Mathf.Lerp(1f, 0.92f, strength);
        Color shiftColor = Color.Lerp(new Color(1f, 0.78f, 0.3f), Color.white, strength * 0.35f);

        background.color = Color.Lerp(_restingColor, new Color(1f, 0.78f, 0.3f), 0.72f);
        sequence.Append(_rect.DOLocalRotate(_restingRotation.eulerAngles + new Vector3(0f, 0f, signed * angle), _motionDuration * 0.35f).SetEase(Ease.OutQuad));
        sequence.Join(_rect.DOScale(new Vector3(_restingScale.x * widthScale, _restingScale.y * heightScale, _restingScale.z), _motionDuration * 0.35f));
        sequence.Join(background.DOColor(shiftColor, _motionDuration * 0.35f));
        sequence.Append(_rect.DOLocalRotate(_restingRotation.eulerAngles, _motionDuration * 0.65f).SetEase(Ease.OutElastic));
        sequence.Join(_rect.DOScale(_restingScale, _motionDuration * 0.65f).SetEase(Ease.OutElastic));
        sequence.Join(background.DOColor(_restingColor, _motionDuration));
        sequence.OnComplete(RestorePose);
        return sequence;
    }

    private Sequence Begin(Image background)
    {
        Cancel();
        CapturePose();
        _background = background;
        _restingColor = background.color;
        _motion = DOTween.Sequence().SetUpdate(true);
        return _motion;
    }

    // 완료 콜백 없이 Tween을 중단하고 밝아진 배경을 원래 점유 상태의 색으로 복구한다.
    public void Cancel()
    {
        if (_motion != null && _motion.IsActive()) _motion.Kill();
        _motion = null;
        RestorePose();
    }

    private void CapturePose()
    {
        if (_rect == null) 
            return;

        _restingScale = _rect.localScale;
        _restingRotation = _rect.localRotation;
    }

    private void RestorePose()
    {
        if (_rect != null)
        {
            _rect.localScale = _restingScale == Vector3.zero ? Vector3.one : _restingScale;
            _rect.localRotation = _restingRotation;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (_background != null) _background.color = _restingColor;
        _background = null;
    }

    // 슬롯 재사용이나 씬 종료로 비활성화·파괴되어도 진행 중인 연출이 남지 않게 한다.
    private void OnDisable() { Cancel(); }
    private void OnDestroy() { Cancel(); }
}
