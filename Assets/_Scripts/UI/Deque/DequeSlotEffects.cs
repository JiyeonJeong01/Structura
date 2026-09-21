using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>슬롯의 배경색 강조와 연출 중단 시 원상 복구를 담당한다.</summary>
public class DequeSlotEffects : MonoBehaviour
{
    [SerializeField] private float _pulseDuration = 0.35f;

    // 연속 조작 시 기존 Tween을 중단하고 당시의 기본 배경색으로 되돌리기 위한 상태
    private Tween _pulse;
    private Image _background;
    private Color _restingColor;

    // 데이터 변경은 즉시 완료한다. 색상만 연출하므로 입력 잠금이나 완료 대기가 필요 없다.
    public void Pulse(Image background)
    {
        Cancel();
        _background = background;
        _restingColor = background.color;
        background.color = Color.Lerp(_restingColor, Color.white, 0.5f);
        // Time.timeScale과 무관하게 UI 강조가 끝나도록 unscaled 시간으로 재생한다.
        _pulse = background.DOColor(_restingColor, _pulseDuration).SetUpdate(true);
    }

    // 완료 콜백 없이 Tween을 중단하고 밝아진 배경을 원래 점유 상태의 색으로 복구한다.
    public void Cancel()
    {
        if (_pulse != null && _pulse.IsActive()) _pulse.Kill();
        _pulse = null;
        if (_background != null) _background.color = _restingColor;
        _background = null;
    }

    // 슬롯 재사용이나 씬 종료로 비활성화·파괴되어도 진행 중인 연출이 남지 않게 한다.
    private void OnDisable() { Cancel(); }
    private void OnDestroy() { Cancel(); }
}
