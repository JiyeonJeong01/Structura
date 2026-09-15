using Sirenix.OdinInspector;
using UnityEngine;

public class HashTableController : MonoBehaviour
{
    [SerializeField] private bool printDebug = true;
    [SerializeField] private int initialCapacity = 8;
    [SerializeField] private string inputKey;
    [SerializeField] private string inputValue;

    [ShowInInspector, ReadOnly]
    private string debugStats;

    [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true, DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
    private string[] debugBuckets;

    private readonly HashTable<string, string> _hashTable = new HashTable<string, string>();

    private void Awake()
    {
        Initialize();
    }

    [Button]
    public void Initialize()
    {
        if (initialCapacity <= 0)
        {
            initialCapacity = 8;
        }

        _hashTable.Initialize(initialCapacity);
        RefreshDebugView();

        if (printDebug)
        {
            Debug.Log($"HashTable initialized. Capacity: {_hashTable.Capacity}");
            Debug.Log(GetDebugLog());
        }
    }

    [Button]
    public void Add()
    {
        _hashTable.Insert(inputKey, inputValue);
        RefreshDebugView();

        if (printDebug)
        {
            Debug.Log($"Add key: {inputKey}, value: {inputValue} / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}");
            Debug.Log(GetDebugLog());
        }
    }

    [Button]
    public void Find()
    {
        Find(inputKey);
    }

    public bool Find(string key)
    {
        bool found = _hashTable.Find(key, out string value);
        RefreshDebugView();

        if (printDebug)
        {
            Debug.Log(found
                ? $"Find key: {key}, value: {value}"
                : $"Find key: {key}, not found");
            Debug.Log(GetDebugLog());
        }

        return found;
    }

    [Button]
    public void Remove()
    {
        Remove(inputKey);
    }

    public bool Remove(string key)
    {
        bool removed = _hashTable.Remove(key);
        RefreshDebugView();

        if (printDebug)
        {
            Debug.Log(removed
                ? $"Remove key: {key} / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}"
                : $"Remove key: {key}, not found");
            Debug.Log(GetDebugLog());
        }

        return removed;
    }

    [Button]
    public void Clear()
    {
        _hashTable.Clear();
        RefreshDebugView();

        if (printDebug)
        {
            Debug.Log($"Clear / Count: {_hashTable.Count}, Capacity: {_hashTable.Capacity}, LoadFactor: {_hashTable.LoadFactor}");
            Debug.Log(GetDebugLog());
        }
    }

    [Button]
    private void RefreshDebugView()
    {
        debugStats = $"Count: {_hashTable.Count} / Capacity: {_hashTable.Capacity} / LoadFactor: {_hashTable.LoadFactor:0.00}";
        debugBuckets = _hashTable.GetDebugBuckets();
    }

    private string GetDebugLog()
    {
        return $"{debugStats}\n{string.Join("\n", debugBuckets)}";
    }
}
