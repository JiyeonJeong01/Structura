using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Heap 트리의 고정 노드 하나를 표시한다.</summary>
public class HeapNodeUIView : MonoBehaviour
{
    public RectTransform Rect => (RectTransform)transform;
    public string DisplayValue => _valueText.text;

    [SerializeField] private TMP_Text _indexText;
    [SerializeField] private TMP_Text _valueText;
    [SerializeField] private TMP_Text _stateText;   // CURRENT : sift 진행 중인 노드, COMPARE : CURRENT와 비교 중인 노드

    [SerializeField] private Image _background;
    [SerializeField] private Outline _outline;

    [SerializeField] private Color _occupiedColor = new Color(0.12f, 0.26f, 0.34f); // 블루그린
    [SerializeField] private Color _emptyColor = new Color(0.045f, 0.065f, 0.095f); // 남색
    [SerializeField] private Color _compareColor = new Color(1f, 0.78f, 0.3f);      // 노란색
    [SerializeField] private Color _currentColor = new Color(0.42f, 0.82f, 1f);     // 하늘색

    [FoldoutGroup("Animation")]
    [SerializeField, Tooltip("새 노드가 등장할 때 시작 크기 배율입니다. 1보다 작으면 작게 시작합니다.")]
    private float _appearStartScale = 0.65f;

    [FoldoutGroup("Animation")]
    [SerializeField, Tooltip("새 노드가 등장할 때 시작 투명도입니다.")]
    private float _appearStartAlpha = 0.25f;

    [FoldoutGroup("Animation")]
    [SerializeField, Tooltip("등장 중 원래 크기보다 살짝 커지는 배율입니다.")]
    private float _appearOvershootScale = 1.08f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("작고 흐린 노드가 커지고 선명해지는 시간입니다.")]
    private float _appearGrowDuration = 0.16f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("커진 노드가 원래 크기로 돌아오는 시간입니다.")]
    private float _appearSettleDuration = 0.16f;

    [FoldoutGroup("Animation")]
    [SerializeField, Tooltip("비교 강조 pulse에서 커지는 배율입니다.")]
    private float _pulseScale = 1.08f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("비교 강조 pulse가 커지는 시간입니다.")]
    private float _pulseGrowDuration = 0.12f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("비교 강조 pulse가 원래 크기로 돌아오는 시간입니다.")]
    private float _pulseSettleDuration = 0.16f;

    private Sequence _effect;
    private CanvasGroup _canvasGroup;
    private Vector3 _restingScale;

    // Refresh 시, 값을 바인딩 해 UI 상태 업데이트
    public void Bind(int index, object value, bool occupied, bool compare, bool current)
    {
        StopEffects();

        _indexText.text = $"[{index}]";
        _valueText.text = occupied ? ValueParser.Format(value) : "empty";

        _background.color = occupied ? _occupiedColor : _emptyColor;

        // CURRENT, COMPARE 노드라면 효과 적용
        SetHighlight(compare, current);

        // Occupied에 따른 투명도 적용
        SetValueVisible(true);
    }

    public void SetHighlight(bool compare, bool current)
    {
        _stateText.text = current ? "CURRENT" : compare ? "COMPARE" : string.Empty;
        _stateText.color = current ? _currentColor : _compareColor;
        _outline.enabled = compare || current;
        _outline.effectColor = current ? _currentColor : _compareColor;
    }

    public void SetValueVisible(bool visible)
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _canvasGroup.alpha = visible ? 1f : 0.28f;
    }

    // 반투명 상태에서 커지며 등장
    public Tween AnimateAppear()
    {
        var effect = BeginEffect();
        Rect.localScale = _restingScale * _appearStartScale;
        _canvasGroup.alpha = _appearStartAlpha;
        effect.Append(Rect.DOScale(_restingScale * _appearOvershootScale, _appearGrowDuration).SetEase(Ease.OutBack));
        effect.Join(_canvasGroup.DOFade(1f, _appearGrowDuration));
        effect.Append(Rect.DOScale(_restingScale, _appearSettleDuration).SetEase(Ease.OutQuad));
        effect.OnComplete(RestorePose);
        return effect;
    }

    // 기존 노드 크기 잠시 키우며 강조
    public Tween AnimatePulse()
    {
        var effect = BeginEffect();
        effect.Append(Rect.DOScale(_restingScale * _pulseScale, _pulseGrowDuration).SetEase(Ease.OutQuad));
        effect.Append(Rect.DOScale(_restingScale, _pulseSettleDuration).SetEase(Ease.OutQuad));
        effect.OnComplete(RestorePose);
        return effect;
    }

    private Sequence BeginEffect()
    {
        StopEffects();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _restingScale = Rect.localScale == Vector3.zero ? Vector3.one : Rect.localScale;
        _effect = DOTween.Sequence().SetUpdate(true);
        return _effect;
    }

    // 해당 Node를 기본 상태로 되돌린다.
    private void RestorePose()
    {
        Rect.localScale = _restingScale == Vector3.zero ? Vector3.one : _restingScale;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    public void StopEffects()
    {
        if (_effect != null && _effect.IsActive())
            _effect.Kill();

        _effect = null;
        RestorePose();
    }

    private void OnDisable()
    {
        StopEffects();
    }
}
