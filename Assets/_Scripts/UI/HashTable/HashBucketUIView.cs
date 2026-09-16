using System.Collections.Generic;
using UnityEngine;

public class HashBucketUIView<TKey, TValue> : UIContainer<HashEntry<TKey, TValue>>
{
    [SerializeField] private int bucketIndex;

    public int BucketIndex => bucketIndex;

    public void SetBucketIndex(int index)
    {
        bucketIndex = index;
    }

    public override void Refresh(IReadOnlyList<HashEntry<TKey, TValue>> data)
    {
    }

    public override void Clear()
    {
    }
}
