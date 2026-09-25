public sealed class DoublyLinkedListIterator<T>
{
    internal DoublyLinkedList<T> Owner { get; }
    internal DoublyLinkedListNode<T> Node { get; }

    // 삭제 또는 Clear 후에는 Node.Owner가 null이 되어 IsValid가 false가 된다.
    public bool IsValid => Node != null && ReferenceEquals(Node.Owner, Owner);

    internal DoublyLinkedListIterator(DoublyLinkedList<T> owner, DoublyLinkedListNode<T> node)
    {
        Owner = owner;
        Node = node;
    }
}
