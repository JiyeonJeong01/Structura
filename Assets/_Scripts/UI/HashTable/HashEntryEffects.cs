using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HashEntryEffects : MonoBehaviour
{
    [SerializeField] private float _travelDuration = 0.55f;
    [SerializeField] private float _pulseScale = 1.22f;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Image _background;
    private Sequence _motion; // 이 엔트리에서 진행 중인 연출을 한 번에 중단하기 위해 보관한다.
    private RectTransform _home; // EffectRoot 이동 전에 속해 있던 슬롯이다.
    private Vector3 _homePosition; // 원래 슬롯 기준의 anchoredPosition을 보관한다.
    private Color _restingColor; // 흰색 플래시 이후 돌아갈 기본색 또는 검색 강조색이다.

    // 이전 연출을 정리하고 현재 UI 상태에 맞는 색과 투명도로 초기화한다.
    public void ResetVisual(Image image, Color color)
    {
        Cancel();
        _rect = (RectTransform)transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _background = image;
        _restingColor = color;
        _background.color = color;
        _canvasGroup.alpha = 1;
    }

    // 복귀 위치를 저장하고 게임 시간 배율에 영향받지 않는 연출을 준비한다.
    private Sequence Begin()
    {
        Cancel();
        _home = (RectTransform)_rect.parent;
        _homePosition = _rect.anchoredPosition3D;
        _motion = DOTween.Sequence().SetUpdate(true);

        return _motion;
    }

    // HEAD 근처에서 나타난 엔트리를 예약된 슬롯 위치까지 이동시킨다.
    public Tween FlyIn(RectTransform effectRoot, Vector3 origin)
    {
        var sequence = Begin();
        // 슬롯은 레이아웃에 남겨 자리를 확보하고 실제 엔트리만 자유 이동 루트로 옮긴다.
        _rect.SetParent(effectRoot, true);
        // 월드 위치를 유지한 채 부모를 바꿔 EffectRoot 기준의 도착 좌표를 얻는다.
        var destination = _rect.anchoredPosition;
        _rect.position = origin;
        _rect.localScale = Vector3.one * 0.25f;
        _rect.localRotation = Quaternion.Euler(0, 0, -12);
        _canvasGroup.alpha = 0;
        _background.color = Color.Lerp(_restingColor, Color.white, 0.8f);
        sequence.Append(_rect.DOScale(0.85f, 0.12f).SetEase(Ease.OutBack));
        sequence.Join(_canvasGroup.DOFade(1, 0.12f));
        sequence.Append(_rect.DOAnchorPos(destination, _travelDuration).SetEase(Ease.OutCubic));
        sequence.Join(_rect.DOScale(1, _travelDuration).SetEase(Ease.OutBack));
        sequence.Join(_rect.DOLocalRotate(Vector3.zero, _travelDuration));
        sequence.Join(_background.DOColor(_restingColor, _travelDuration));
        sequence.OnComplete(RestorePose);
        return sequence;
    }

    // 잠시 확대하고 흰색으로 밝힌 뒤 원래 크기와 상태색으로 복귀한다.
    public Tween Pulse()
    {
        var sequence = Begin();
        sequence.Append(_rect.DOScale(_pulseScale, 0.16f).SetEase(Ease.OutBack));
        sequence.Join(_background.DOColor(Color.Lerp(_restingColor, Color.white, 0.85f), 0.12f));
        sequence.Append(_rect.DOScale(1, 0.38f).SetEase(Ease.OutElastic));
        sequence.Join(_background.DOColor(_restingColor, 0.3f));
        sequence.OnComplete(RestorePose);
        return sequence;
    }

    // 짧게 강조한 뒤 회전과 축소, 페이드로 삭제를 표현한다.
    public Tween Disappear()
    {
        var sequence = Begin();
        sequence.Append(_rect.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad));
        sequence.Join(_background.DOColor(new Color(1, 0.45f, 0.35f), 0.1f));
        sequence.Append(_rect.DOScale(0, 0.3f).SetEase(Ease.InBack));
        sequence.Join(_canvasGroup.DOFade(0, 0.26f));
        sequence.Join(_rect.DOLocalRotate(new Vector3(0, 0, 14), 0.3f));
        // 버킷 갱신이 슬롯을 비울 때까지 투명한 최종 상태를 유지한다.
        return sequence;
    }

    // 완료 콜백을 실행하지 않고 연출을 중단한 뒤 재사용 가능한 상태로 복구한다.
    public void Cancel()
    {
        if (_motion != null && _motion.IsActive()) _motion.Kill();
        _motion = null;
        RestorePose();
    }

    // 이동했던 엔트리를 원래 슬롯에 돌려놓고 크기, 회전, 투명도, 색을 복원한다.
    private void RestorePose()
    {
        if (_rect == null) return;
        if (_home != null)
        {
            if (_rect.parent != _home) _rect.SetParent(_home, false);
            _rect.anchoredPosition3D = _homePosition;
        }
        _home = null;
        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;
        if (_canvasGroup != null) _canvasGroup.alpha = 1;
        if (_background != null) _background.color = _restingColor;
    }

    private void OnDisable() { Cancel(); }
    private void OnDestroy() { Cancel(); }
}
