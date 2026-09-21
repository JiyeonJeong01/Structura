using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>읽기 전용 snapshot을 받아 전체 MAP과 Start/Finish 카드를 표시한다.</summary>
public class DequeUIView : MonoBehaviour
{
    // 세로 map 목록을 구성할 부모와 행 프리팹
    [SerializeField] private RectTransform _mapRoot;
    [SerializeField] private DequeMapRowUIView _rowPrefab;
    // MAP 왼쪽의 iterator 상세 정보 UI
    [SerializeField] private Text _startText;
    [SerializeField] private Text _finishText;

    // 리스트 index를 실제 map index와 일치시켜 행을 재사용한다.
    private readonly List<DequeMapRowUIView> _rows = new List<DequeMapRowUIView>();

    // start/finish를 포함한 하나의 snapshot으로 모든 행을 같은 시점의 상태로 그린다.
    public void Refresh(Deque<object>.Snapshot snapshot, int changedStart = 0, int changedCount = 0, int selectedIndex = -1)
    {
        // map이 확장된 만큼만 행을 추가한다.
        while (_rows.Count < snapshot.MapSize)
        {
            var row = Instantiate(_rowPrefab, _mapRoot);
            row.name = "Map " + _rows.Count;
            _rows.Add(row);
        }
        // 새 세션에서 map이 작아지면 남는 행은 숨기고 다음 확장 때 재사용한다.
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].gameObject.SetActive(i < snapshot.MapSize);
            if (i < snapshot.MapSize)
                _rows[i].Refresh(snapshot.Rows[i], snapshot, changedStart, changedCount, selectedIndex);
        }
        _startText.text = IteratorText(snapshot, snapshot.Start, false);
        _finishText.text = IteratorText(snapshot, snapshot.Finish, true);
    }

    // iterator의 네 필드와 실제 접근 위치를 표시한다. finish와 빈 상태의 start는 역참조하지 않는다.
    private static string IteratorText(Deque<object>.Snapshot snapshot, Deque<object>.IteratorSnapshot iterator, bool finish)
    {
        string value = finish ? "past-the-end (no value)" : snapshot.Count == 0 ? "empty (no value)"
            : HashValueParser.Format(snapshot.Rows[iterator.Node].Slots[iterator.Curr].Value);
        return $"Node   {iterator.Node}\nCurr    {iterator.Curr}\nFirst    {iterator.First}\nLast    {iterator.Last} (exclusive)\n\nmap[{iterator.Node}][{iterator.Curr}]\n{value}";
    }
}
