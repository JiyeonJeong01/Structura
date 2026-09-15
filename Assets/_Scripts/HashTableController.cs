using UnityEngine;

public class HashTableController : MonoBehaviour
{
    [SerializeField] private bool printDebug = true;
    [SerializeField] private int initialCapacity = 8;
    [SerializeField] private string inputKey;
    [SerializeField] private string inputValue;

    private readonly HashTable<string, string> _hashTable = new HashTable<string, string>();

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialCapacity <= 0)
        {
            initialCapacity = 8;
        }

        _hashTable.Initialize(initialCapacity);

        if (printDebug)
        {
            Debug.Log($"HashTable initialized. Capacity: {_hashTable.Capacity}");
        }
    }

    public void Add()
    {
        _hashTable.Insert(inputKey, inputValue);

        if (printDebug)
        {
            Debug.Log($"Add key: {inputKey}, value: {inputValue} / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}");
        }
    }

    public void Find()
    {
        Find(inputKey);
    }

    public bool Find(string key)
    {
        bool found = _hashTable.Find(key, out string value);

        if (printDebug)
        {
            Debug.Log(found
                ? $"Find key: {key}, value: {value}"
                : $"Find key: {key}, not found");
        }

        return found;
    }

    public void Remove()
    {
        Remove(inputKey);
    }

    public bool Remove(string key)
    {
        bool removed = _hashTable.Remove(key);

        if (printDebug)
        {
            Debug.Log(removed
                ? $"Remove key: {key} / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}"
                : $"Remove key: {key}, not found");
        }

        return removed;
    }

    public void Clear()
    {
        _hashTable.Clear();

        if (printDebug)
        {
            Debug.Log($"Clear / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}");
        }
    }
}
