using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HashBucketUIView : UIContainer<HashEntry<object, object>>
{
    public int BucketIndex => _bucketIndex;
    public RectTransform EffectRoot => _effectRoot;

    [SerializeField] private int _bucketIndex;
    [FormerlySerializedAs("headText"), SerializeField] private TMP_Text _headText;
    [SerializeField] private RectTransform _entryLayoutRoot;    // 고정 크기의 슬롯을 왼쪽부터 정렬한다.
    [SerializeField] private RectTransform _effectRoot;         // 애니메이션 등 효과 넣을 때 루트
    [SerializeField] private RectTransform _slotTemplate;       // 부족한 슬롯을 복제하는 비활성 원본이다.
    [SerializeField] private HashEntryUIView _entryPrefab;

    // 비활성 엔트리도 보관하여 이후 삽입에서 슬롯과 함께 재사용한다.
    private readonly List<HashEntryUIView> _entries = new List<HashEntryUIView>();
    private Tween _headShake;
    private Vector2 _headPosition; // 셰이크 종료 또는 중단 시 복구할 HEAD 텍스트의 위치이다.

    public void SetBucketIndex(int index)
    {
        _bucketIndex = index;
    }

    private void Awake() { _headPosition = _headText.rectTransform.anchoredPosition; }

    // 현재 표시 중인 엔트리에서 키가 같은 UI를 찾는다.
    public HashEntryUIView FindEntry(object key)
    {
        return _entries.Find(entry => entry.gameObject.activeInHierarchy && Equals(entry.Data.Key, key));
    }

    // 새 엔트리는 이동시키고 기존 키의 값 갱신은 강조 효과로 표시한다.
    public Tween AnimateInsert(object key, bool isNew)
    {
        var entry = FindEntry(key);
        if (entry == null) 
            return null;

        if (!isNew) 
            return entry.Effects.Pulse();

        // 삽입 후 같은 버킷에 둘 이상 있으면 충돌 피드백을 재생한다.
        int count = _entries.FindAll(item => item.gameObject.activeInHierarchy).Count;
        if (count > 1)
        {
            _headShake = _headText.rectTransform.DOShakeAnchorPos(0.42f, new Vector2(12, 6), 18)
                .SetUpdate(true).OnComplete(() => _headText.rectTransform.anchoredPosition = _headPosition);
        }

        return entry.Effects.FlyIn(_effectRoot, _headText.rectTransform.position);
    }

    // 버킷과 엔트리의 연출을 중단하고 원래 배치로 되돌린다.
    public void StopEffects()
    {
        if (_headShake != null && _headShake.IsActive()) 
            _headShake.Kill();
        _headText.rectTransform.anchoredPosition = _headPosition;

        foreach (var entry in _entries)
            if (entry != null)
                entry.Effects.Cancel();
    }

    private void OnDisable()
    {
        StopEffects();
    }

    // 실제 체인 순서에 맞춰 슬롯을 재사용하고 표시 내용을 갱신한다.
    public override void Refresh(IReadOnlyList<HashEntry<object, object>> data)
    {
        while (_entries.Count < data.Count)
        {
            var slot = Instantiate(_slotTemplate, _entryLayoutRoot);
            slot.name = "Slot " + _entries.Count;
            slot.gameObject.SetActive(true);
            _entries.Add(Instantiate(_entryPrefab, slot));
        }
        for (int i = 0; i < _entries.Count; i++)
        {
            // 남는 슬롯은 숨겨 레이아웃 계산에서 제외하고 다음 삽입에 재사용한다.
            if (i < data.Count)
            {
                _entries[i].transform.parent.gameObject.SetActive(true);
                _entries[i].Bind(data[i]);
            }
            else
            {
                _entries[i].transform.parent.gameObject.SetActive(false);
            }
        }
        _headText.text = $"[{_bucketIndex}] HEAD\n" + (data.Count == 0 ? "-> null" : "->");
    }

    // 일치하는 키만 강조하고 나머지는 기본 상태로 되돌린다.
    public void Highlight(object key)
    {
        foreach (var entry in _entries)
            if (entry.gameObject.activeInHierarchy)
                entry.SetState(key != null && Equals(entry.Data.Key, key) 
                    ? UIEntryState.Found 
                    : UIEntryState.Normal);
    }

    public override void Clear()
    {
        Refresh(System.Array.Empty<HashEntry<object, object>>());
    }
}
