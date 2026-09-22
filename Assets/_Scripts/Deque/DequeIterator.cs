/// <summary>Deque의 map node와 block 내부 위치를 보관하는 자료구조용 iterator.</summary>
public class DequeIterator
{
    public int Curr = 0;   // block 내 위치
    public int First = 0;  // block의 시작 위치
    public int Last = 0;   // block의 끝 위치 (배타적 상한)
    public int Node = 0;   // map 안의 block index

    public DequeIterator()
    {
    }

    // start와 finish가 같은 위치에서 시작해도 서로 독립적으로 이동하도록 필드 값만 복사한다.
    public DequeIterator(DequeIterator other)
    {
        Curr = other.Curr;
        First = other.First;
        Last = other.Last;
        Node = other.Node;
    }
}
