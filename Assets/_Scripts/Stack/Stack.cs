
/// <summary>
/// Deque의 뒷방향 연산만 사용하여 LIFO 구조를 구현한다. 
/// </summary>
public class Stack<T>
{
    // STL에서 Stack은 Deque의 컨테이너 어댑터
    private Deque<T> _deque = new Deque<T>();

    public int Count => _deque.Count;

    public void Initialize(int mapSize = 4, int blockSize = 8)
    {
        _deque.Initialize(mapSize, blockSize);
    }

    public void Push(T value)
    {
        _deque.PushBack(value);
    }

    public bool Pop(out T value)
    {
        return _deque.PopBack(out value);
    }

    public bool TryGetTop(out T value)
    {
        return _deque.TryGetBack(out value);
    }

    // 시각화는 원소를 변경하지 않고 바닥부터 top까지의 현재 순서만 읽는다.
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
