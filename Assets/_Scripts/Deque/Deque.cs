using System;
using System.Text;

/// <summary>map이 고정 크기 block들을 참조하고, [start, finish) 범위에 원소를 보관하는 Deque.</summary>
public class Deque<T>
{
    public int Count { get; private set; } = 0;
    public int MapSize { get; private set; }
    public int BlockSize { get; private set; }


    private T[][] _map;

    // [start, finish)
    private DequeIterator _start;    // 첫 번째 실제 원소
    private DequeIterator _finish;   // 마지막 실제 원소 다음 위치

    public void Initialize(int mapSize = 4, int blockSize = 8)
    {
        if (mapSize < 1) throw new ArgumentOutOfRangeException(nameof(mapSize));
        if (blockSize < 1) throw new ArgumentOutOfRangeException(nameof(blockSize));

        Count = 0;
        MapSize = mapSize;
        BlockSize = blockSize;

        // 첫 원소는 가운데 쯤에 위치 
        _map = new T[MapSize][];
        _map[MapSize / 2] = new T[BlockSize];

        _start = new DequeIterator();

        _start.Curr = BlockSize / 2;
        _start.First = 0;
        _start.Last = BlockSize;
        _start.Node = MapSize / 2;

        // 빈 Deque는 start == finish 위치이며, 아직 실제 원소를 가리키지 않는다.
        _finish = new DequeIterator(_start);
    }

    // start를 이전 슬롯으로 옮긴 뒤 저장하여 새 원소가 첫 원소가 되도록 한다.
    public void PushFront(T value)
    {
        // start 이동 후 원소 저장
        MoveStartPrev();
        _map[_start.Node][_start.Curr] = value;
        Count++;
    }

    // 현재 finish에 저장한 뒤 finish를 다음 빈 위치로 이동한다.
    public void PushBack(T value)
    {
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
        value = default;

        if (IsEmpty())
            return false;

        value = _map[_start.Node][_start.Curr];

        return true;
    }

    public bool TryGetBack(out T value)
    {
        value = default;

        if (IsEmpty())
            return false;

        int node = _finish.Node;
        int curr = _finish.Curr - 1;

        // finish가 block의 첫 칸이면 마지막 원소는 이전 block의 마지막 칸에 있다.
        if (curr < 0)
        {
            node--;
            curr = BlockSize - 1;
        }

        value = _map[node][curr];

        return true;
    }

    // 논리 index를 map/block 내부 위치로 변환해 접근하고 범위 밖의 index는 예외로 알린다.
    public T this[int index]
    {
        get
        {
            int node = 0, localIndex = 0;
            if (CalcIndex(index, out node, out localIndex))
                return _map[node][localIndex];

            throw new IndexOutOfRangeException($"Deque Out Of Range! index : {index}");
        }

        set
        {
            int node = 0, localIndex = 0;
            if (CalcIndex(index, out node, out localIndex))
            {
                _map[node][localIndex] = value;
                return;
            }

            throw new IndexOutOfRangeException($"Deque Out Of Range! index : {index}");
        }
    }

    // 양끝은 Push로 처리하고, 중간 삽입만 뒤쪽 원소들을 이동시킨다. 중간 조작 비용은 O(n)이다.
    public bool InsertAt(int index, T value)
    {
        if (index < 0 || index > Count)
            return false;

        // 맨 앞/뒤에 삽입
        if (index == 0)
        {
            PushFront(value);
            return true;
        }

        if (index == Count)
        {
            PushBack(value);
            return true;
        }

        // 마지막 값을 복제해 끝 슬롯부터 확보한다. 뒤에서부터 옮겨야 아직 옮기지 않은 값을 덮지 않는다.
        PushBack(this[Count - 1]);

        for (int i = Count - 2; i > index; --i)
            this[i] = this[i - 1];

        this[index] = value;
        return true;
    }

    // 삭제할 값을 보관하고 뒤쪽 원소를 앞으로 당긴다. 양끝 삭제는 Pop으로 처리한다.
    public bool RemoveAt(int index, out T value)
    {
        value = default;

        if (index < 0 || index >= Count)
            return false;

        // 맨 앞/뒤에서 삭제 
        if (index == 0)
            return PopFront(out value);

        if (index == Count - 1)
            return PopBack(out value);

        value = this[index];

        // index + 1 번째 원소부터 앞으로 한 칸씩 당겨오기
        for (int i = index; i < Count - 1; ++i)
            this[i] = this[i + 1];

        // 이동 후 마지막에 중복으로 남은 값을 제거하고 finish와 Count를 함께 갱신한다.
        PopBack(out _);
        return true;
    }

    // 현재 map/block 크기는 유지하되 block들을 비우고 양끝 iterator를 가운데로 초기화한다.
    public void Clear()
    {
        Initialize(MapSize, BlockSize);
    }

    // block 할당 여부와 관계없이 실제 원소 개수로 빈 상태를 판단한다.
    public bool IsEmpty()
    {
        return Count == 0;
    }

    // 내부 iterator를 노출하지 않고 필드 값이 같은 복사본을 반환한다.
    // 호출자가 반환값을 변경해도 Deque의 start 위치에는 영향을 주지 않는다.
    public DequeIterator GetStartIterator()
    {
        if (_start == null) 
            throw new InvalidOperationException("Initialize the deque first.");

        return new DequeIterator(_start);
    }

    // finish도 독립된 복사본을 반환한다. finish는 실제 원소 다음 위치다.
    public DequeIterator GetFinishIterator()
    {
        if (_finish == null) 
            throw new InvalidOperationException("Initialize the deque first.");

        return new DequeIterator(_finish);
    }

    // block 배열 자체를 노출하지 않고 지정한 map node의 할당 여부만 반환한다.
    public bool HasBlock(int node)
    {
        if (_map == null)
            throw new InvalidOperationException("Initialize the deque first.");

        return _map[node] != null;
    }

    // 자료구조의 현재 map 참조, 값, S/F 위치를 문자열로 확인한다.
    public string GetDebugView()
    {
        if (_map == null) return "Deque is not initialized.";
        var builder = new StringBuilder();
        builder.AppendLine($"Count: {Count} / Map: {MapSize} / Block: {BlockSize}");
        builder.AppendLine($"Start: Node={_start.Node}, Curr={_start.Curr}, First={_start.First}, Last={_start.Last}");
        builder.AppendLine($"Finish: Node={_finish.Node}, Curr={_finish.Curr}, First={_finish.First}, Last={_finish.Last}");
        int startOffset = _start.Node * BlockSize + _start.Curr;
        for (int node = 0; node < MapSize; node++)
        {
            builder.Append($"[{node}] -> ");
            if (_map[node] == null)
            {
                builder.AppendLine("null");
                continue;
            }
            for (int slot = 0; slot < BlockSize; slot++)
            {
                int index = node * BlockSize + slot - startOffset;
                builder.Append('[');
                if (node == _start.Node && slot == _start.Curr) builder.Append("S ");
                if (node == _finish.Node && slot == _finish.Curr) builder.Append("F ");
                builder.Append(index >= 0 && index < Count ? $"{index}:{_map[node][slot]}" : "empty");
                builder.Append("] ");
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private void MoveStartPrev()
    {
        if (_start.Curr > 0)
        {
            _start.Curr--;
            return;
        }

        // map의 맨 앞까지 사용한 경우 먼저 확장하여 이전 block을 둘 자리를 만든다.
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
        // 마지막 슬롯에 원소를 넣는 순간 다음 블록을 할당된다.
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

        // 크기 1의 map도 앞뒤에 최소 한 노드씩 여유를 둔다.
        MapSize = Math.Max(4, MapSize * 2);
        _map = new T[MapSize][];

        int newStartIdx = Math.Max(1, oldMapSize / 2);
        int oldIdx = oldStartNode;
        int newIdx = newStartIdx;

        // start부터 finish까지의 block 참조만 새 map으로 옮긴다.
        while (oldIdx <= oldLastNode)
            _map[newIdx++] = oldMap[oldIdx++];

        _start.Node = newStartIdx;
        _finish.Node = newStartIdx + (oldFinishNode - oldStartNode);
    }

    // 필요한 node에만 block을 할당한다. 이미 할당된 배열은 덮어쓰지 않고 재사용한다.
    private void EnsureBlock(int node)
    {
        if (_map[node] == null)
            _map[node] = new T[BlockSize];
    }

    // start 기준의 논리 index를 실제 map node와 block 내부 슬롯 번호로 변환한다.
    private bool CalcIndex(int index, out int node, out int localIdx)
    {
        node = localIdx = 0;

        if (index < 0 || index >= Count)
            return false;

        // start block의 첫 칸부터 센 거리다. 몫은 건너갈 block 수, 나머지는 도착 block의 슬롯이다.
        int offset = _start.Curr + index;
        node = _start.Node + offset / BlockSize;
        localIdx = offset % BlockSize;

        return node >= 0 && node < MapSize && _map[node] != null;
    }
}
