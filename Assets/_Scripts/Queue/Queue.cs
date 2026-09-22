public class Queue<T>
{
    private Deque<T> _deque = new Deque<T>();

    public int Count => _deque.Count;

    public void Initialize(int mapSize = 4, int blockSize = 8)
    {
        _deque.Initialize(mapSize, blockSize);
    }

    public void Enqueue(T value)
    {
        _deque.PushBack(value);
    }

    public bool Dequeue(out T value)
    {
        return _deque.PopFront(out value);
    }

    public bool TryGetFront(out T value)
    {
        return _deque.TryGetFront(out value);
    }

    // 시각화는 원소를 변경하지 않고 front부터 rear까지의 현재 순서만 읽는다.
    public T GetValueAt(int index)
    {
        return _deque[index];
    }

    public bool IsEmpty()
    {
        return _deque.IsEmpty();
    }

    public void Clear()
    {
        _deque.Clear();
    }
}
