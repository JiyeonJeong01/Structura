using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class HashTableUIView : UIContainer<IReadOnlyList<HashEntry<object, object>>>
{
    [SerializeField] private RectTransform _bucketRoot;
    [SerializeField] private HashBucketUIView _bucketPrefab;

    // 리스트 인덱스를 실제 버킷 인덱스와 일치시켜 유지한다.
    private readonly List<HashBucketUIView> _buckets = new List<HashBucketUIView>();

    // 현재 용량에 맞춰 버킷 UI를 확보하고 각 버킷의 체인을 갱신한다.
    public override void Refresh(IReadOnlyList<IReadOnlyList<HashEntry<object, object>>> data)
    {
        // bucket 오브젝트 확보
        while (_buckets.Count < data.Count)
        {
            var bucket = Instantiate(_bucketPrefab, _bucketRoot);
            bucket.SetBucketIndex(_buckets.Count);
            bucket.name = "Bucket " + _buckets.Count;
            _buckets.Add(bucket);
        }

        // 버킷 정보 갱신
        for (int i = 0; i < _buckets.Count; i++)
        {
            if (i < data.Count)
            {
                _buckets[i].gameObject.SetActive(true);
                _buckets[i].Refresh(data[i]);
            }
            else
            {
                _buckets[i].gameObject.SetActive(false);
                _buckets[i].Clear();
            }
        }
    }

    // find 에서 모든 Entry Highlight Off 이후 find = true 시, On 한다.
    public void Highlight(object key)
    {
        foreach (var bucket in _buckets) 
            bucket.Highlight(key);
    }

    // 삽입 또는 rehash 이후 실제 슬롯 위치를 확정하고 대상 엔트리를 연출한다.
    public Tween AnimateInsert(object key, bool isNew)
    {
        // 같은 프레임에 생성된 슬롯도 최종 레이아웃 좌표를 사용하도록 갱신한다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bucketRoot);

        // 기존 값을 덮어쓰는 경우에만 삽입 효과(Pulse)를 넣는다.
        foreach (var bucket in _buckets)
            if (bucket.FindEntry(key) != null) 
                return bucket.AnimateInsert(key, isNew);
        return null;
    }

    // 키에 대응하는 엔트리에서 삭제 또는 검색 강조 연출을 재생한다.
    public Tween AnimateEntry(object key, bool removing)
    {
        foreach (var bucket in _buckets)
        {
            var entry = bucket.FindEntry(key);
            if (entry != null) 
                return removing ? entry.Effects.Disappear() : entry.Effects.Pulse();
        }
        return null;
    }

    // 표시 중인 모든 엔트리의 삭제 연출을 동시에 재생한다.
    public Tween AnimateClear()
    {
        // 빈 테이블에서는 빈 Sequence를 만들지 않고 즉시 종료할 수 있게 한다.
        Sequence sequence = null;
        foreach (var bucket in _buckets)
            foreach (var entry in bucket.GetComponentsInChildren<HashEntryUIView>())
            {
                if (sequence == null)
                    sequence = DOTween.Sequence().SetUpdate(true);
                sequence.Join(entry.Effects.Disappear());
            }

        return sequence;
    }

    // 모든 버킷의 연출을 중단하여 자유 이동 중인 엔트리까지 제자리로 복구한다.
    public void StopEffects()
    {
        foreach (var bucket in _buckets)
            if (bucket != null) 
                bucket.StopEffects();
    }

    // 버킷 UI는 유지하고 내부 엔트리만 비운다.
    public override void Clear()
    {
        foreach (var bucket in _buckets) 
            bucket.Clear();
    }
}
