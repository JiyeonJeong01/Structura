internal sealed class DoublyLinkedListNode<T>
{
    internal T Value;
    internal DoublyLinkedListNode<T> Previous;
    internal DoublyLinkedListNode<T> Next;
    internal DoublyLinkedList<T> Owner;
    internal bool IsSentinel;

    internal DoublyLinkedListNode(DoublyLinkedList<T> owner, T value, bool isSentinel = false)
    {
        Owner = owner;
        Value = value;
        IsSentinel = isSentinel;
    }
}
