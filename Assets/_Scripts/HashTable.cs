using System;
using System.Collections.Generic;
using System.Text;

public class HashTable<TKey, TValue>
{
    public int Capacity { get; private set; }
    public int Count { get; private set; }
    public float LoadFactor => (float)Count / Capacity;

    private const float RehashThreshold = 0.75f;
    private HashEntry<TKey, TValue>[] _buckets;


    public void Initialize(int capacity = 8)
    {
        Capacity = capacity;
        Count = 0;
        _buckets = new HashEntry<TKey, TValue>[capacity];
    }

    public void Insert(TKey key, TValue value)
    {
        int bucketIndex = GetBucketIndex(key);

        if (_buckets[bucketIndex] == null)
        {
            _buckets[bucketIndex] = new HashEntry<TKey, TValue>(key, value);
            Count++;
            RehashIfNeeded();
            return;
        }

        HashEntry<TKey, TValue> current = _buckets[bucketIndex];

        while (true)
        {
            // 중복 키 방지
            if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
            {
                current.Value = value;
                return;
            }

            if (current.Next == null)
            {
                current.Next = new HashEntry<TKey, TValue>(key, value);
                Count++;
                RehashIfNeeded();
                return;
            }

            current = current.Next;
        }
    }

    public bool Find(TKey key, out TValue value)
    {
        int bucketIndex = GetBucketIndex(key);
        HashEntry<TKey, TValue> current = _buckets[bucketIndex];

        while (current != null)
        {
            if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
            {
                value = current.Value;
                return true;
            }

            current = current.Next;
        }

        // 미발견 시 기본값 반환
        value = default;
        return false;
    }

    public bool Remove(TKey key)
    {
        int bucketIndex = GetBucketIndex(key);
        HashEntry<TKey, TValue> current = _buckets[bucketIndex];
        HashEntry<TKey, TValue> prev = null;

        while (current != null)
        {

            if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
            {
                // 삭제할 원소가 첫 번째 엔트리
                if (prev == null)
                    _buckets[bucketIndex] = current.Next;
                else 
                    prev.Next = current.Next;

                Count--;
                return true;
            }

            prev = current;
            current = current.Next;
        }

        return false;
    }

    public void Clear()
    {
        // 첫 번째 엔트리만 진입점을 끊으면, 이후 엔트리들은 GC의 수거 대상이 된다.
        for (int i = 0; i < Capacity; i++)
        {
            _buckets[i] = null;
        }

        Count = 0;
    }

    public string GetDebugView()
    {
        if (_buckets == null)
        {
            return "HashTable is not initialized.";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Count: {Count} / Capacity: {Capacity} / LoadFactor: {LoadFactor:0.00}");

        for (int i = 0; i < Capacity; i++)
        {
            builder.Append($"[{i}] ");

            HashEntry<TKey, TValue> current = _buckets[i];

            if (current == null)
            {
                builder.AppendLine("empty");
                continue;
            }

            while (current != null)
            {
                builder.Append($"({FormatValue(current.Key)} : {FormatValue(current.Value)})");

                if (current.Next != null)
                {
                    builder.Append(" -> ");
                }

                current = current.Next;
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    public string[] GetDebugBuckets()
    {
        if (_buckets == null)
        {
            return new[] { "HashTable is not initialized." };
        }

        string[] debugBuckets = new string[Capacity];

        for (int i = 0; i < Capacity; i++)
        {
            StringBuilder builder = new StringBuilder();
            HashEntry<TKey, TValue> current = _buckets[i];

            if (current == null)
            {
                debugBuckets[i] = "empty";
                continue;
            }

            while (current != null)
            {
                builder.Append($"({FormatValue(current.Key)} : {FormatValue(current.Value)})");

                if (current.Next != null)
                {
                    builder.Append(" -> ");
                }

                current = current.Next;
            }

            debugBuckets[i] = builder.ToString();
        }

        return debugBuckets;
    }

    private int GetHash(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return key.GetHashCode();
    }

    private int GetBucketIndex(TKey key)
    {
        int hash = GetHash(key);
        int index = hash % Capacity;

        if (index < 0)
            index += Capacity;

        return index;
    }

    private void RehashIfNeeded()
    {
        if (LoadFactor >= RehashThreshold)
        {
            Rehash();
        }
    }

    private void Rehash()
    {
        HashEntry<TKey, TValue>[] oldBuckets = _buckets;
        int oldCapacity = Capacity;

        Capacity *= 2;
        _buckets = new HashEntry<TKey, TValue>[Capacity];

        for (int i = 0; i < oldCapacity; ++i)
        {
            HashEntry<TKey, TValue> oldCurrent = oldBuckets[i];

            while (oldCurrent != null)
            {
                HashEntry<TKey, TValue> nextOld = oldCurrent.Next;
                oldCurrent.Next = null;

                int bucketIndex = GetBucketIndex(oldCurrent.Key);
                AppendEntry(bucketIndex, oldCurrent);

                oldCurrent = nextOld;
            }
        }

    }

    private void AppendEntry(int bucketIndex, HashEntry<TKey, TValue> entry)
    {
        if (_buckets[bucketIndex] == null)
        {
            _buckets[bucketIndex] = entry;
            return;
        }

        HashEntry<TKey, TValue> current = _buckets[bucketIndex];

        while (current.Next != null)
        {
            current = current.Next;
        }

        current.Next = entry;
    }

    private static string FormatValue<T>(T value)
    {
        return value == null ? "null" : value.ToString();
    }
}
