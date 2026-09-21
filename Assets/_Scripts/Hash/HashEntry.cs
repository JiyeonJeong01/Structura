using UnityEngine;

public class HashEntry<TKey, TValue>
{
    public TKey Key;
    public TValue Value;
    public HashEntry<TKey, TValue> Next;

    public HashEntry(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}
