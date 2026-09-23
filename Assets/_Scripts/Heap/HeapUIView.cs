using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Heap의 고정 트리 노드와 내부 배열 슬롯을 표시하고 DOTween 연출을 담당한다.</summary>
public class HeapUIView : MonoBehaviour
{
    // 노드, 원소 집합
    [SerializeField] private HeapNodeUIView[] _nodes;
    [SerializeField] private HeapArraySlotUIView[] _arraySlots;
    [SerializeField] private Image[] _links;

    [SerializeField] private RectTransform _effectRoot;

    // 노드 간선 색깔
    [SerializeField] private Color _activeLinkColor = new Color(0.28f, 0.64f, 0.72f, 0.9f);     // 청록색
    [SerializeField] private Color _inactiveLinkColor = new Color(0.08f, 0.13f, 0.18f, 0.45f);  // 남색

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("Swap 또는 Pop에서 ghost 값이 목적지까지 이동하는 시간입니다.")]
    private float _ghostMoveDuration = 0.42f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("원소가 하나뿐일 때 Pop ghost가 사라지는 시간입니다.")]
    private float _singlePopFadeDuration = 0.28f;

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(0f), Tooltip("여러 원소 Pop에서 root 값이 사라지는 시간입니다.")]
    private float _rootPopFadeDuration = 0.2f;

    [FoldoutGroup("Animation")]
    [SerializeField, Tooltip("이동 연출용 ghost 카드의 배경색입니다.")]
    private Color _ghostColor = new Color(0.13f, 0.64f, 0.55f, 0.95f);

    [FoldoutGroup("Animation")]
    [SerializeField, MinValue(1f), Tooltip("이동 연출용 ghost 값 텍스트 크기입니다.")]
    private float _ghostFontSize = 20f;

    public int Capacity => _nodes.Length;

    public void Refresh(Heap<object> heap, int currentIndex = -1, int compareIndex = -1)
    {
        // 전체 노드, 원소 순회하며 상태 갱신
        for (int i = 0; i < _nodes.Length; i++)
        {
            bool occupied = i < heap.Count;
            object value = occupied ? heap.GetValueAt(i) : null;
            bool compare = i == compareIndex;
            bool current = i == currentIndex;

            _nodes[i].Bind(i, value, occupied, compare, current);
            _arraySlots[i].Bind(i, value, occupied, compare, current);
        }

        for (int i = 0; i < _links.Length; i++)
            _links[i].color = i + 1 < heap.Count ? _activeLinkColor : _inactiveLinkColor;
    }

    public void SetHighlights(int currentIndex, int compareIndex)
    {
        for (int i = 0; i < _nodes.Length; i++)
        {
            bool compare = i == compareIndex;
            bool current = i == currentIndex;
            _nodes[i].SetHighlight(compare, current);
            _arraySlots[i].SetHighlight(compare, current);
        }
    }

    public Tween AnimateAppear(int index)
    {
        if (!IsValidIndex(index))
            return null;

        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(_nodes[index].AnimateAppear());
        sequence.Join(_arraySlots[index].AnimateAppear());
        return sequence;
    }

    public Tween AnimatePairPulse(int currentIndex, int compareIndex)
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        if (IsValidIndex(currentIndex))
        {
            sequence.Join(_nodes[currentIndex].AnimatePulse());
            sequence.Join(_arraySlots[currentIndex].AnimatePulse());
        }

        if (IsValidIndex(compareIndex))
        {
            sequence.Join(_nodes[compareIndex].AnimatePulse());
            sequence.Join(_arraySlots[compareIndex].AnimatePulse());
        }

        return sequence;
    }

    // 노드, 원소가 서로 교환(swap)된다.
    // 원본 UI는 숨기고, ghost UI를 만들어 연출한다.
    public Tween AnimateSwap(int firstIndex, int secondIndex)
    {
        if (!IsValidIndex(firstIndex) || !IsValidIndex(secondIndex))
            return null;

        Canvas.ForceUpdateCanvases();
        // 원본 노드, 원소를 잠시 흐리게 숨긴다.
        SetPairVisible(firstIndex, secondIndex, false);

        var sequence = DOTween.Sequence().SetUpdate(true);
        var ghosts = new List<RectTransform>();

        // 노드, 원소를 swap 한다.
        AddMovingPair(sequence, _nodes[firstIndex].Rect, _nodes[secondIndex].Rect,
            _nodes[firstIndex].DisplayValue, _nodes[secondIndex].DisplayValue, ghosts);
        AddMovingPair(sequence, _arraySlots[firstIndex].Rect, _arraySlots[secondIndex].Rect,
            _arraySlots[firstIndex].DisplayValue, _arraySlots[secondIndex].DisplayValue, ghosts);

        sequence.OnComplete(() => SetPairVisible(firstIndex, secondIndex, true));
        sequence.OnKill(() =>
        {
            DestroyGhosts(ghosts);
            SetPairVisible(firstIndex, secondIndex, true);
        });

        return sequence;
    }

    // root를 제거하고, 마지막 값이 root로 올라온다.
    // 원본 UI는 숨기고, ghost UI를 만들어 연출한다.
    public Tween AnimatePopMove(int lastIndex)
    {
        if (!IsValidIndex(lastIndex))
            return null;

        Canvas.ForceUpdateCanvases();
        var sequence = DOTween.Sequence().SetUpdate(true);

        // 하나 남은 원소를 pop한 경우
        if (lastIndex == 0)
        {
            // 루트 노드, 원소를 흐리게 만든 상태에서 아예 안 보이게 한다.
            _nodes[0].SetValueVisible(false);
            _arraySlots[0].SetValueVisible(false);
            var nodeGhost = CreateGhost(_nodes[0].Rect, _nodes[0].DisplayValue);
            var slotGhost = CreateGhost(_arraySlots[0].Rect, _arraySlots[0].DisplayValue);
            
            // alpha값 0으로
            sequence.Join(nodeGhost.GetComponent<CanvasGroup>().DOFade(0f, _singlePopFadeDuration));
            sequence.Join(slotGhost.GetComponent<CanvasGroup>().DOFade(0f, _singlePopFadeDuration));

            sequence.OnComplete(() =>
            {
                _nodes[0].SetValueVisible(true);
                _arraySlots[0].SetValueVisible(true);
            });
            sequence.OnKill(() =>
            {
                DestroyGhosts(nodeGhost, slotGhost);
                _nodes[0].SetValueVisible(true);
                _arraySlots[0].SetValueVisible(true);
            });

            return sequence;
        }

        SetPairVisible(0, lastIndex, false);
        var rootNodeGhost = CreateGhost(_nodes[0].Rect, _nodes[0].DisplayValue);
        var rootSlotGhost = CreateGhost(_arraySlots[0].Rect, _arraySlots[0].DisplayValue);

        var ghosts = new List<RectTransform> { rootNodeGhost, rootSlotGhost };

        // 루트 노드, 원소는 투명해지며 퇴장한다.
        sequence.Join(rootNodeGhost.GetComponent<CanvasGroup>().DOFade(0f, _rootPopFadeDuration));
        sequence.Join(rootSlotGhost.GetComponent<CanvasGroup>().DOFade(0f, _rootPopFadeDuration));

        // 마지막 노드, 원소는 루트 위치로 이동한다.
        AddOneWayMove(sequence, _nodes[lastIndex].Rect, _nodes[0].Rect, _nodes[lastIndex].DisplayValue, ghosts);
        AddOneWayMove(sequence, _arraySlots[lastIndex].Rect, _arraySlots[0].Rect, _arraySlots[lastIndex].DisplayValue, ghosts);
        
        sequence.OnComplete(() =>
        {
            SetPairVisible(0, lastIndex, true);
        });
        sequence.OnKill(() =>
        {
            DestroyGhosts(ghosts);
            SetPairVisible(0, lastIndex, true);
        });
        return sequence;
    }

    public void StopEffects()
    {
        foreach (var node in _nodes)
            node.StopEffects();
        foreach (var slot in _arraySlots)
            slot.StopEffects();
        for (int i = _effectRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(_effectRoot.GetChild(i).gameObject);
    }

    private void AddMovingPair(Sequence sequence, RectTransform first, RectTransform second,
        string firstValue, string secondValue, List<RectTransform> ghosts)
    {
        var firstGhost = CreateGhost(first, firstValue);
        var secondGhost = CreateGhost(second, secondValue);
        ghosts.Add(firstGhost);
        ghosts.Add(secondGhost);

        // Ghost의 중심 좌표를 _effectRoot의 좌표로 변환한다.
        Vector2 firstPosition = AnchoredInEffectRoot(first);
        Vector2 secondPosition = AnchoredInEffectRoot(second);

        // first -> second, second -> first로 이동
        sequence.Join(firstGhost.DOAnchorPos(secondPosition, _ghostMoveDuration).SetEase(Ease.InOutQuad));
        sequence.Join(secondGhost.DOAnchorPos(firstPosition, _ghostMoveDuration).SetEase(Ease.InOutQuad));
    }

    private void AddOneWayMove(Sequence sequence, RectTransform from, RectTransform to,
        string value, List<RectTransform> ghosts)
    {
        var ghost = CreateGhost(from, value);
        ghosts.Add(ghost);

        // from -> to
        sequence.Join(ghost.DOAnchorPos(AnchoredInEffectRoot(to), _ghostMoveDuration).SetEase(Ease.InOutQuad));
    }

    // 이동 연출용 Ghost를 생성한다.
    private RectTransform CreateGhost(RectTransform source, string value)
    {
        var root = new GameObject("MovingValue", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        root.layer = gameObject.layer;
        var rect = (RectTransform)root.transform;
        rect.SetParent(_effectRoot, false);
        rect.sizeDelta = source.rect.size;
        rect.anchoredPosition = AnchoredInEffectRoot(source);
        var image = root.GetComponent<Image>();
        image.color = _ghostColor;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 4f);
        textRect.offsetMax = new Vector2(-6f, -4f);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = _ghostFontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return rect;
    }

    // UI RectTransform의 화면상 중심 위치를 _effectRoot 기준 anchoredPosition 좌표로 변환한다.
    private Vector2 AnchoredInEffectRoot(RectTransform target)
    {
        // 타겟의 로컬 중심점을 월드 좌표로 바꾼다.
        Vector3 world = target.TransformPoint(target.rect.center);

        // 월드 좌표를 특정 Transform의 로컬 좌표로 바꾼다.
        Vector3 local = _effectRoot.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    private void SetPairVisible(int firstIndex, int secondIndex, bool visible)
    {
        _nodes[firstIndex].SetValueVisible(visible);
        _nodes[secondIndex].SetValueVisible(visible);
        _arraySlots[firstIndex].SetValueVisible(visible);
        _arraySlots[secondIndex].SetValueVisible(visible);
    }

    private static void DestroyGhosts(params RectTransform[] ghosts)
    {
        foreach (var ghost in ghosts)
            if (ghost != null)
                Object.Destroy(ghost.gameObject);
    }

    private static void DestroyGhosts(List<RectTransform> ghosts)
    {
        foreach (var ghost in ghosts)
            if (ghost != null)
                Object.Destroy(ghost.gameObject);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _nodes.Length && index < _arraySlots.Length;
    }

}
