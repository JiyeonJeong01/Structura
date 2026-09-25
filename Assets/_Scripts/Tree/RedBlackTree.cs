using System;

public class RedBlackTree<T>
{
    private enum NodeColor
    {
        Red,
        Black
    }

    private sealed class Node
    {
        public T Value;
        public NodeColor Color;
        public Node Parent;
        public Node Left;
        public Node Right;

        public Node(T value, NodeColor color)
        {
            Value = value;
            Color = color;
        }
    }

    private Node _root;
    private Node _nil;

    public int Count { get; private set; }

    public void Initialize()
    {
        throw new NotImplementedException();
    }

    public bool Insert(T value)
    {
        throw new NotImplementedException();
    }

    public bool Find(T value, out T foundValue)
    {
        throw new NotImplementedException();
    }

    public bool Remove(T value)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }
}
