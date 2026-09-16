using System.Collections.Generic;

public class HashTableUIView<TKey, TValue> : UIContainer<IReadOnlyList<HashEntry<TKey, TValue>>>
{
    public override void Refresh(IReadOnlyList<IReadOnlyList<HashEntry<TKey, TValue>>> data)
    {
    }

    public override void Clear()
    {
    }
}
