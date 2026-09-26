using System.Collections.Generic;

public class DoublyLinkedList <T>
{
    private DoublyLinkedListNode<T> _head;
    private DoublyLinkedListNode<T> _tail;

    public int Count { get; private set; }

    public void Initialize()
    {
        Clear();

        _head = new DoublyLinkedListNode<T>(this, default, true);
        _tail = new DoublyLinkedListNode<T>(this, default, true);

        _head.Next = _tail;
        _tail.Previous = _head;
    }

    public bool IsEmpty()
    {
        return Count == 0;
    }

    public DoublyLinkedListIterator<T> GetHeadIterator()
    {
        if (_head == null || _head.Next == _tail)
            return null;

        return CreateIterator(_head.Next);
    }

    public DoublyLinkedListIterator<T> GetTailIterator()
    {
        if (_tail == null || _tail.Previous == _head)
            return null;

        return CreateIterator(_tail.Previous);
    }

    public DoublyLinkedListIterator<T> AddFirst(T value)
    {
        return InsertBetween(_head, _head.Next, value);
    }

    public DoublyLinkedListIterator<T> AddLast(T value)
    {
        return InsertBetween(_tail.Previous, _tail, value);
    }

    public DoublyLinkedListIterator<T> InsertBefore(DoublyLinkedListIterator<T> iterator, T value)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> currentNode))
            return null;

        return InsertBetween(currentNode.Previous, currentNode, value);
    }

    public DoublyLinkedListIterator<T> InsertAfter(DoublyLinkedListIterator<T> iterator, T value)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> currentNode))
            return null;

        return InsertBetween(currentNode, currentNode.Next, value);
    }

    public bool TryRemoveFirst(out T value)
    {
        if (_head == null || _head.Next == _tail)
        {
            value = default;
            return false;
        }

        value = _head.Next.Value;
        RemoveNode(_head.Next);
        return true;
    }

    public bool TryRemoveLast(out T value)
    {
        if (_tail == null || _tail.Previous == _head)
        {
            value = default;
            return false;
        }

        value = _tail.Previous.Value;
        RemoveNode(_tail.Previous);
        return true;
    }

    public bool Remove(DoublyLinkedListIterator<T> iterator)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> node))
            return false;

        RemoveNode(node);
        return true;
    }

    public bool TryGetValue(DoublyLinkedListIterator<T> iterator, out T value)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> node))
        {
            value = default;
            return false;
        }

        value = node.Value;
        return true;
    }

    public bool TryGetNext(DoublyLinkedListIterator<T> iterator, out DoublyLinkedListIterator<T> nextIterator)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> node) || node.Next == _tail)
        {
            nextIterator = null;
            return false;
        }

        nextIterator = CreateIterator(node.Next);
        return true;
    }

    public bool TryGetPrevious(DoublyLinkedListIterator<T> iterator, out DoublyLinkedListIterator<T> previousIterator)
    {
        if (!TryGetNode(iterator, out DoublyLinkedListNode<T> node) || node.Previous == _head)
        {
            previousIterator = null;
            return false;
        }

        previousIterator = CreateIterator(node.Previous);
        return true;
    }

    public DoublyLinkedListIterator<T> Find(T value)
    {
        if (_head == null)
            return null;

        for (DoublyLinkedListNode<T> currentNode = _head.Next;
             currentNode != _tail;
             currentNode = currentNode.Next)
        {
            if (Utils.Compare(currentNode.Value, value) == CompareRes.Equal)
                return CreateIterator(currentNode);
        }

        return null;
    }

    public bool Contains(T value)
    {
        return Find(value) != null;
    }

    public void Clear()
    {
        if (_head == null)
        {
            Count = 0;
            return;
        }

        // 무효화
        DoublyLinkedListNode<T> currentNode = _head.Next;
        while (currentNode != _tail)
        {
            DoublyLinkedListNode<T> nextNode = currentNode.Next;
            InvalidateNode(currentNode);
            currentNode = nextNode;
        }

        _head.Next = _tail;
        _tail.Previous = _head;
        Count = 0;
    }

    private DoublyLinkedListIterator<T> InsertBetween(DoublyLinkedListNode<T> previousNode,
                                                       DoublyLinkedListNode<T> nextNode,
                                                       T value)
    {
        DoublyLinkedListNode<T> newNode = new DoublyLinkedListNode<T>(this, value);

        // <- [newNode] -> 연결
        newNode.Previous = previousNode;
        newNode.Next = nextNode;

        // -> [newNode] <- 연결
        previousNode.Next = newNode;
        nextNode.Previous = newNode;

        Count++;
        return CreateIterator(newNode);
    }

    private bool TryGetNode(DoublyLinkedListIterator<T> iterator, out DoublyLinkedListNode<T> node)
    {
        node = null;

        if (iterator == null || !ReferenceEquals(iterator.Owner, this))
            return false;

        node = iterator.Node;
        return node != null && !node.IsSentinel && ReferenceEquals(node.Owner, this);
    }

    private DoublyLinkedListIterator<T> CreateIterator(DoublyLinkedListNode<T> node)
    {
        // _head나 _tail은 이터레이터를 생성하지 않는다.
        return node == null || node.IsSentinel
            ? null
            : new DoublyLinkedListIterator<T>(this, node);
    }

    private void RemoveNode(DoublyLinkedListNode<T> node)
    {
        node.Previous.Next = node.Next;
        node.Next.Previous = node.Previous;

        // 기존 iterator는 Owner가 null인 노드를 가리켜 무효 상태가 된다.
        InvalidateNode(node);
        Count--;
    }

    private static void InvalidateNode(DoublyLinkedListNode<T> node)
    {
        node.Previous = null;
        node.Next = null;
        node.Owner = null;
    }
}
