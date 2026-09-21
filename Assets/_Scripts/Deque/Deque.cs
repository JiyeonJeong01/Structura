using System;

public class Deque<T>
{
    #region Iterator

    public class Iterator
    {
        public int Curr = 0;   // block 내 위치
        public int First = 0;  // block의 시작 위치
        public int Last = 0;   // block의 끝 위치
        public int Node = 0;   // map 안의 block index

        public Iterator()
        {
        }

        public Iterator(Iterator other)
        {
            Curr = other.Curr;
            First = other.First;
            Last = other.Last;
            Node = other.Node;
        }
    }

    #endregion

    public int Count { get; private set; } = 0;
    public int MapSize { get; private set; }
    public int BlockSize { get; private set; }


    private T[][] _map;

    // [start, finish)
    private Iterator _start;    // 첫 번째 실제 원소
    private Iterator _finish;   // 마지막 실제 원소 다음 위치

    public void Initialize(int mapSize = 4, int blockSize = 8)
    {
        Count = 0;
        MapSize = mapSize;
        BlockSize = blockSize;

        // 첫 원소는 가운데 쯤에 위치 
        _map = new T[MapSize][];
        _map[MapSize / 2] = new T[BlockSize];

        _start = new Iterator();

        _start.Curr = BlockSize / 2;
        _start.First = 0;
        _start.Last = BlockSize;
        _start.Node = MapSize / 2;

        _finish = new Iterator(_start);

    }

    public void PushFront(T value)
    {
        if (_start == null)
            throw new ArgumentNullException();

        // start 이동 후 원소 저장
        MoveStartPrev();
        _map[_start.Node][_start.Curr] = value;
        Count++;
    }

    public void PushBack(T value)
    {
        if (_finish == null)
            throw new ArgumentNullException();

        // 원소 저장 후 finish 이동
        EnsureBlock(_finish.Node);
        _map[_finish.Node][_finish.Curr] = value;
        Count++;

        MoveFinishNext();
    }

    public bool PopFront(out T value)
    {
        if (_start == null)
            throw new ArgumentNullException();

        if (!TryGetFront(out value))
            return false;

        _map[_start.Node][_start.Curr] = default;

        // _start 이터레이터 이동
        if (!MoveStartNext())
            return false;

        Count--;

        return true;
    }

    public bool PopBack(out T value)
    {
        if (_finish == null)
            throw new ArgumentNullException();

        if (!TryGetBack(out value))
            return false;

        // _finish 이터레이터 이동
        if (!MoveFinishPrev())
            return false;

        _map[_finish.Node][_finish.Curr] = default;

        Count--;

        return true;
    }


    public bool TryGetFront(out T value)
    {
        if (_start == null)
            throw new ArgumentNullException();

        value = default;

        if (IsEmpty())
            return false;

        value = _map[_start.Node][_start.Curr];

        return true;
    }

    public bool TryGetBack(out T value)
    {
        if (_finish == null)
            throw new ArgumentNullException();

        value = default;

        if (IsEmpty())
            return false;

        int node = _finish.Node;
        int curr = _finish.Curr - 1;

        if (curr < 0)
        {
            node--;
            curr = BlockSize - 1;
        }

        value = _map[node][curr];

        return true;
    }

    public void Clear()
    {
        Initialize(MapSize, BlockSize);
    }

    public string GetDebugView()
    {
        throw new NotImplementedException();
    }

    public bool IsEmpty()
    {
        return Count == 0;
    }

    private void MoveStartPrev()
    {
        if (_start.Curr > 0)
        {
            _start.Curr--;
            return;
        }

        if (_start.Node == 0)
            RecenterMap();

        _start.Node--;
        _start.Curr = BlockSize - 1;
        _start.First = 0;
        _start.Last = BlockSize;

        // 기존에 할당됐던 배열 재사용하여 성능 보존
        EnsureBlock(_start.Node);
    }

    private bool MoveStartNext()
    {
        if (IsEmpty())
            return false;

        // 현재 Block이 비어서 _start를 다음 Block으로 옮겨야 한다.
        if (++_start.Curr >= BlockSize)
        {
            _start.Curr = 0;
            _start.First = 0;
            _start.Last = BlockSize;
            _start.Node++;
        }

        return true;
    }

    private void MoveFinishNext()
    {
        if (_finish.Curr + 1 < BlockSize)
        {
            _finish.Curr++;
            return;
        }

        if (_finish.Node + 1 >= MapSize)
        {
            RecenterMap();
        }

        _finish.Node++;
        _finish.Curr = 0;
        _finish.First = 0;
        _finish.Last = BlockSize;

        // 기존에 할당됐던 배열 재사용하여 성능 보존
        EnsureBlock(_finish.Node);
    }

    private bool MoveFinishPrev()
    {
        if (IsEmpty())
            return false;

        if (_finish.Curr > 0)
        {
            _finish.Curr--;
            return true;
        }

        _finish.Node--;
        _finish.Curr = BlockSize - 1;
        _finish.First = 0;
        _finish.Last = BlockSize;

        return true;
    }

    // 기존 블록 참조 배열이 중앙에 위치하도록 한다.
    // [a][b][c][d] -> [ ][ ][a][b][c][d][ ][ ]
    private void RecenterMap()
    {
        T[][] oldMap = _map;
        int oldMapSize = MapSize;
        int oldStartNode = _start.Node;
        int oldFinishNode = _finish.Node;
        int oldLastNode = oldFinishNode;

        // finish가 다음 블록의 첫 칸을 가리키는 상태면 마지막 사용 노드는 그 이전 노드다.
        if (_finish.Curr == 0)
            oldLastNode--;

        MapSize *= 2;
        _map = new T[MapSize][];

        int newStartIdx = oldMapSize / 2;
        int oldIdx = oldStartNode;
        int newIdx = newStartIdx;

        while (oldIdx <= oldLastNode)
            _map[newIdx++] = oldMap[oldIdx++];

        _start.Node = newStartIdx;
        _finish.Node = newStartIdx + (oldFinishNode - oldStartNode);
    }

    private void EnsureBlock(int node)
    {
        if (_map[node] == null)
            _map[node] = new T[BlockSize];
    }
}
