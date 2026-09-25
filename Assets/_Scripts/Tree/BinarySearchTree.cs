public class BinarySearchTree<T>
{
    private sealed class Node
    {
        public T Value;
        public Node Left;
        public Node Right;

        public Node(T value)
        {
            Value = value;
        }
    }

    private Node _root;

    public int Count { get; private set; }

    public void Initialize()
    {
        _root = null;
        Count = 0;
    }

    public bool Insert(T value)
    {
        if (_root == null)
        {
            _root = new Node(value);
            Count++;
            return true;
        }
        
        Node foundNode = FindNode(value, out Node parentNode);

        // 중복 불허
        if (foundNode != null)
            return false;

        Node newNode = new Node(value);
        if (Utils.Compare(value, parentNode.Value) == CompareRes.Less)
            parentNode.Left = newNode;
        else
            parentNode.Right = newNode;

        Count++;

        return true;
    }

    public bool Contains(T value)
    {
        return FindNode(value, out _) != null;
    }

    public bool Remove(T value)
    {
        Node targetNode = FindNode(value, out Node parentNode);
        if (targetNode == null)
            return false;

        // 자식이 둘이면 후계자의 값을 옮긴 뒤, 자식이 하나 이하인 후계자만 제거한다.
        if (targetNode.Left != null && targetNode.Right != null)
        {
            Node successorParent = targetNode;
            Node successor = targetNode.Right;

            while (successor.Left != null)
            {
                successorParent = successor;
                successor = successor.Left;
            }

            targetNode.Value = successor.Value;
            targetNode = successor;
            parentNode = successorParent;
        }

        // 자식이 0, 1개인 경우에만 Left의 null 여부도 고려하고, 
        // 자식이 2개라면 무조건 Right가 대입된다.
        Node replacementNode = targetNode.Left ?? targetNode.Right;
        ReplaceChild(parentNode, targetNode, replacementNode);

        Count--;
        return true;
    }

    public void Clear()
    {
        _root = null;
        Count = 0;
    }

    // parentNode : 노드 값이 없다면 새 노드가 붙을 부모가 담긴다. 
    private Node FindNode(T value, out Node parentNode)
    {
        parentNode = null;
        Node currentNode = _root;

        while (currentNode != null)
        {
            CompareRes result = Utils.Compare(value, currentNode.Value);
            if (result == CompareRes.Equal)
                return currentNode;

            parentNode = currentNode;

            // 노드 타고 내려가기
            currentNode = result == CompareRes.Less
                ? currentNode.Left
                : currentNode.Right;
        }

        return null;
    }

    private void ReplaceChild(Node parentNode, Node currentNode, Node replacementNode)
    {
        if (parentNode == null)
        {
            _root = replacementNode;
        }
        else if (parentNode.Left == currentNode)
        {
            parentNode.Left = replacementNode;
        }
        else
        {
            parentNode.Right = replacementNode;
        }
    }
}
