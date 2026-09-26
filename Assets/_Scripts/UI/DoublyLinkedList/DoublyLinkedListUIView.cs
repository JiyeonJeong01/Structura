using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UISettings = DoublyLinkedListUISettings;

// 화면 노드의 수명과 간선 재연결 순서를 관리한다.
public class DoublyLinkedListUIView : MonoBehaviour
{
    [SerializeField] private RectTransform _nodeRoot;
    [SerializeField] private DoublyLinkedListNodeUIView _nodePrefab;

    [SerializeField] private DoublyLinkedListNodeUIView _head;
    [SerializeField] private DoublyLinkedListNodeUIView _tail;
    [SerializeField] private DoublyLinkedListLinkUIView[] _links;

    [SerializeField] private TMP_Text _emptyText;
    [SerializeField] private TMP_Text _searchText;
    [SerializeField] private Image _missOverlay;    // Find 실패 시 화면 붉게 하는 효과

    // _head와 _tail은 제외
    private readonly List<DoublyLinkedListNodeUIView> _nodes = new List<DoublyLinkedListNodeUIView>();  
    private int _active = UISettings.NoSelection;

    // 데이터 개수와 다른 화면 노드는 다시 만들고 중간 연출 상태를 모두 정상화한다.
    public void Refresh(List<string> values, int active)
    {
        // 데이터와 UI의 개수가 어긋나는 경우, 데이터(values) 기준 재생성
        if (_nodes.Count != values.Count)
        {
            foreach (var node in _nodes)
                Destroy(node.gameObject);

            _nodes.Clear();

            foreach (string value in values)
                _nodes.Add(CreateNode(value));
        }

        // UI 노드, 간선 설정
        _active = active;
        for (int i = 0; i < _nodes.Count; i++)
        {
            _nodes[i].SetValue(values[i]);
            _nodes[i].Rect.anchoredPosition = Position(i);
            _nodes[i].Group.alpha = 1f;
        }

        for (int i = 0; i < _links.Length; i++)
            _links[i].SetProgress(1f);

        // Tween이 비활성화 될 수 있으니, 안전하게 초기화한다.
        _missOverlay.color = UISettings.MissOverlayColor;

        _emptyText.gameObject.SetActive(_nodes.Count == 0);

        ResetHighlights();
        UpdateLinks();
    }

    // 삽입 또는 삭제에 영향을 받는 연결만 끊는다.
    public Tween CollapseLinks(int start, int count)
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        for (int i = start; i < start + count; i++)
            sequence.Join(_links[i].Animate(0f));
        return sequence;
    }

    // 새 노드를 위에서 내려보내고 기존 노드도 최종 위치로 이동시킨다.
    public Tween Insert(int index, string value)
    {
        var node = CreateNode(value);
        _nodes.Insert(index, node);

        // 최종 위치보다  UISettings.NodeVerticalTravel 픽셀 만큼 위에서 시작하여 이동한다.
        node.Rect.anchoredPosition = Position(index) + Vector2.up * UISettings.NodeVerticalTravel;
        node.Group.alpha = 0f;

        _emptyText.gameObject.SetActive(false);

        // 효과 넣기 전 미리 간선 접어두기
        PrepareLinks(index, 2);

        var sequence = MoveNodes();
        sequence.Join(node.Group.DOFade(1f, UISettings.NodeMoveDuration));

        return sequence;
    }

    // 노드를 위로 보내며 두 연결을 동시에 길이 0으로 줄인다.
    public Tween Remove(int index)
    {
        var sequence = DOTween.Sequence().SetUpdate(true);

        // 삭제 대상 노드의 좌우 간선 접기
        sequence.Join(_links[index].Animate(0f));
        sequence.Join(_links[index + 1].Animate(0f));

        // 삭제 대상 노드 위로 이동시키며 페이드
        sequence.Join(_nodes[index].Rect.DOAnchorPosY(
            _nodes[index].Rect.anchoredPosition.y + UISettings.NodeVerticalTravel, UISettings.NodeMoveDuration));
        sequence.Join(_nodes[index].Group.DOFade(0f, UISettings.NodeMoveDuration));

        return sequence;
    }

    // 간선을 접은 뒤 노드들을 짧은 간격으로 위쪽에 흩날리며 지운다.
    public Tween AnimateClear()
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(CollapseLinks(0, _nodes.Count + 1));

        var flySequence = DOTween.Sequence();
        float centerIndex = (_nodes.Count - 1) * 0.5f;
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            float delay = i * UISettings.ClearNodeStagger;
            float driftX = (i - centerIndex) * UISettings.ClearHorizontalSpread;
            Vector2 destination = node.Rect.anchoredPosition
                + new Vector2(driftX, UISettings.ClearVerticalTravel);

            // 위치 이동과 페이드를 같은 시점에 시작하고 노드만 날려 센티널은 남긴다.
            flySequence.Insert(delay, node.Rect.DOAnchorPos(destination, UISettings.ClearNodeDuration)
                .SetEase(Ease.InQuad));
            flySequence.Insert(delay, node.Group.DOFade(0f, UISettings.ClearNodeDuration));
        }

        sequence.Append(flySequence);
        return sequence;
    }

    // 사라진 노드를 제거한 뒤 빈자리를 닫고 앞뒤 사이 연결을 준비한다.
    public Tween CloseGap(int index)
    {
        Destroy(_nodes[index].gameObject);

        _nodes.RemoveAt(index);
        PrepareLinks(index, 1);

        return MoveNodes();
    }

    // 위치 이동이 끝난 뒤 끊긴 연결을 최종 길이까지 펼친다.
    public Tween ExpandLinks()
    {
        UpdateLinks();
        var sequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i <= _nodes.Count; i++)
        {
            if (_links[i].Progress >= 1f)
                continue;

            sequence.Join(_links[i].Animate(1f));
        }

        return sequence;
    }

    // -1은 Head, Count는 Tail이며 그 사이가 실제 데이터 노드다.
    public Tween Pulse(int index, bool found)
    {
        ResetHighlights();
        var node = index < 0 ? 
            _head 
            : index == _nodes.Count 
                ? _tail 
                : _nodes[index];

        return DOTween.Sequence().SetUpdate(true).Append(node.Pulse(found));
    }

    // 찾는 값은 작업 중에만 별도 영역에 표시한다.
    public void ShowSearch(string message)
    {
        _searchText.text = message ?? string.Empty;
        _searchText.gameObject.SetActive(message != null);
    }

    // 화면을 가리지 않는 낮은 알파의 빨간색으로 탐색 실패를 알린다.
    public Tween FlashMiss()
    {
        return DOTween.Sequence().SetUpdate(true)
            .Append(_missOverlay.DOFade(UISettings.MissPeakAlpha, UISettings.MissFadeInDuration))
            .Append(_missOverlay.DOFade(0f, UISettings.MissFadeOutDuration));
    }

    // 기존 선택 표시를 남기고 이전 순회 강조만 해제한다.
    private void ResetHighlights()
    {
        _head.Select(false);
        _tail.Select(false);

        for (int i = 0; i < _nodes.Count; i++)
            _nodes[i].Select(i == _active);

        for (int i = 0; i < _links.Length; i++)
            _links[i].Select(i == _active + 1 && _active >= 0);
    }

    // 씬에 연결된 프리팹으로 노드를 생성한다.
    private DoublyLinkedListNodeUIView CreateNode(string value)
    {
        var node = Instantiate(_nodePrefab, _nodeRoot);
        node.SetValue(value);
        return node;
    }

    // 노드의 중앙 피봇 위치를 반환한다.
    private Vector2 Position(int index)
    {
        float centerX = _nodeRoot.rect.width * 0.5f;

        // [ ] [ ] [ center ] [ ] [ ]
        float offsetX = (index - (_nodes.Count - 1) * 0.5f) * UISettings.NodeCenterSpacing;
        return new Vector2(centerX + offsetX, UISettings.NodeRowY);
    }

    // 연결 인덱스가 바뀐 뒤 새로 이어질 구간만 접어 둔다.
    private void PrepareLinks(int start, int count)
    {
        for (int i = 0; i < _links.Length; i++)
            _links[i].SetProgress(i >= start && i < start + count ? 0f : 1f);

        UpdateLinks();
    }

    // 노드 이동 중에도 기존 연결이 포인터 칸을 따라가도록 매 프레임 갱신한다.
    private Sequence MoveNodes()
    {
        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.AppendInterval(UISettings.NodeMoveDuration);

        // 목표 위치로 이동.
        for (int i = 0; i < _nodes.Count; i++)
            sequence.Join(_nodes[i].Rect.DOAnchorPos(Position(i), UISettings.NodeMoveDuration).SetEase(Ease.InOutSine));

        sequence.OnUpdate(UpdateLinks);

        return sequence;
    }

    // 센티널을 포함한 인접 쌍마다 next/prev 연결의 양 끝을 계산한다.
    private void UpdateLinks()
    {
        for (int i = 0; i < _links.Length; i++)
        {
            // 안 쓰는 간선들은 비활성화
            _links[i].gameObject.SetActive(i <= _nodes.Count);

            if (i > _nodes.Count)
                continue;
            
            // [ 0:_head ]-[ 1 ]-[ 2 ]-[ _tail ] : _nodes.Count = 2
            // 간선 기준 좌, 우 노드를 의미한다.
            var left = i == 0 ? _head : _nodes[i - 1];
            var right = i == _nodes.Count ? _tail : _nodes[i];

            Vector2 leftNodePosition = left.Rect.anchoredPosition;
            Vector2 rightNodePosition = right.Rect.anchoredPosition;

            // 왼쪽 노드의 오른쪽 끝과 오른쪽 노드의 왼쪽 끝을 연결한다.
            Vector2 linkStart = leftNodePosition;
            linkStart.x += left.Rect.rect.xMax;

            Vector2 linkEnd = rightNodePosition;
            linkEnd.x += right.Rect.rect.xMin;

            _links[i].Place(linkStart, linkEnd);
        }
    }
}
