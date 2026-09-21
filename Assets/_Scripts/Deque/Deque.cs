using System;
using System.Collections.Generic;
using System.Text;

/// <summary>map이 고정 크기 block들을 참조하고, [start, finish) 범위에 원소를 보관하는 Deque.</summary>
public class Deque<T>
{
    #region Iterator

    // Node로 block을 선택하고 Curr로 block 내부 슬롯을 가리킨다. Last는 배타적 상한이다.
    public class Iterator
    {
        public int Curr = 0;   // block 내 위치
        public int First = 0;  // block의 시작 위치
        public int Last = 0;   // block의 끝 위치
        public int Node = 0;   // map 안의 block index

        public Iterator()
        {
        }

        // start와 finish가 같은 위치에서 시작해도 서로 독립적으로 이동하도록 필드 값만 복사한다.
        public Iterator(Iterator other)
        {
            Curr = other.Curr;
            First = other.First;
            Last = other.Last;
            Node = other.Node;
        }
    }

    #endregion

    // 내부 Iterator/배열을 노출하지 않는 표시 전용 복사본이다.
    public readonly struct IteratorSnapshot
    {
        public int Curr { get; }
        public int First { get; }
        public int Last { get; }
        public int Node { get; }

        internal IteratorSnapshot(Iterator iterator)
        {
            Curr = iterator.Curr;
            First = iterator.First;
            Last = iterator.Last;
            Node = iterator.Node;
        }
    }

    // 물리 슬롯 위치와 논리 index를 함께 전달한다. 유효 범위 밖의 LogicalIndex는 -1이다.
    public sealed class SlotSnapshot
    {
        public int SlotIndex { get; }
        public int LogicalIndex { get; }
        public bool IsOccupied => LogicalIndex >= 0;
        public T Value { get; }
        public bool IsStart { get; }
        public bool IsFinish { get; }

        internal SlotSnapshot(int slot, int index, T value, bool isStart, bool isFinish)
        {
            SlotIndex = slot;
            LogicalIndex = index;
            Value = value;
            IsStart = isStart;
            IsFinish = isFinish;
        }
    }

    // map 한 행의 block 할당 여부를 전달한다. null block과 할당된 빈 block을 구분한다.
    public sealed class MapRowSnapshot
    {
        public int MapIndex { get; }
        public bool HasBlock { get; }
        public IReadOnlyList<SlotSnapshot> Slots { get; }

        internal MapRowSnapshot(int index, bool hasBlock, SlotSnapshot[] slots)
        {
            MapIndex = index;
            HasBlock = hasBlock;
            Slots = Array.AsReadOnly(slots);
        }
    }

    // 같은 시점의 map, 슬롯, 양끝 iterator 상태를 하나로 묶어 UI에 전달한다.
    public sealed class Snapshot
    {
        public int Count { get; }
        public int MapSize => Rows.Count;
        public int BlockSize { get; }
        public IteratorSnapshot Start { get; }
        public IteratorSnapshot Finish { get; }
        public IReadOnlyList<MapRowSnapshot> Rows { get; }

        internal Snapshot(int count, int blockSize, Iterator start, Iterator finish, MapRowSnapshot[] rows)
        {
            Count = count;
            BlockSize = blockSize;
            Start = new IteratorSnapshot(start);
            Finish = new IteratorSnapshot(finish);
            Rows = Array.AsReadOnly(rows);
        }
    }

    public int Count { get; private set; } = 0;
    public int MapSize { get; private set; }
    public int BlockSize { get; private set; }


    // map의 각 원소는 block 배열의 참조다. 아직 사용하지 않은 위치에는 null이 들어간다.
    private T[][] _map;

    // [start, finish)
    private Iterator _start;    // 첫 번째 실제 원소
    private Iterator _finish;   // 마지막 실제 원소 다음 위치

    // 앞뒤 삽입 공간을 확보하기 위해 가운데 block의 중간 슬롯에서 빈 상태로 시작한다.
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

        _start = new Iterator();

        _start.Curr = BlockSize / 2;
        _start.First = 0;
        _start.Last = BlockSize;
        _start.Node = MapSize / 2;

        // 빈 Deque는 start == finish 위치이며, 아직 실제 원소를 가리키지 않는다.
        _finish = new Iterator(_start);

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

    // 첫 값을 반환하고 슬롯을 비운 뒤 start를 다음 원소로 옮긴다. block 자체는 재사용한다.
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

    // finish 바로 이전의 값을 반환하고, finish를 그 위치로 옮겨 슬롯을 비운다.
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


    // start가 가리키는 첫 값을 읽는다. 빈 상태에서는 배열에 접근하지 않는다.
    public bool TryGetFront(out T value)
    {
        value = default;

        if (IsEmpty())
            return false;

        value = _map[_start.Node][_start.Curr];

        return true;
    }

    // finish는 원소 다음 위치이므로 한 칸 앞을 계산해 마지막 값을 읽는다.
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

    // 유효 범위는 값의 default/null 여부가 아니라 [start, finish)로 판정한다.
    // T 값은 얕은 복사지만 map/slot/iterator 구조는 외부에서 바꿀 수 없다.
    public Snapshot GetSnapshot()
    {
        if (_map == null) throw new InvalidOperationException("Initialize the deque first.");

        var rows = new MapRowSnapshot[MapSize];
        int startOffset = _start.Node * BlockSize + _start.Curr;
        for (int node = 0; node < MapSize; node++)
        {
            // 할당되지 않은 block은 슬롯이 없는 행으로 전달한다.
            var slots = new SlotSnapshot[_map[node] == null ? 0 : BlockSize];
            for (int slot = 0; slot < slots.Length; slot++)
            {
                // map 전체의 물리 슬롯 번호에서 start 위치를 빼면 논리 index가 된다.
                int index = node * BlockSize + slot - startOffset;
                bool occupied = index >= 0 && index < Count;
                slots[slot] = new SlotSnapshot(slot, occupied ? index : -1,
                    occupied ? _map[node][slot] : default,
                    node == _start.Node && slot == _start.Curr,
                    node == _finish.Node && slot == _finish.Curr);
            }
            rows[node] = new MapRowSnapshot(node, _map[node] != null, slots);
        }
        return new Snapshot(Count, BlockSize, _start, _finish, rows);
    }

    // UI와 같은 snapshot 기준으로 map 참조, 값, S/F 위치를 확인할 수 있는 문자열을 만든다.
    public string GetDebugView()
    {
        if (_map == null) return "Deque is not initialized.";
        var snapshot = GetSnapshot();
        var builder = new StringBuilder();
        builder.AppendLine($"Count: {Count} / Map: {MapSize} / Block: {BlockSize}");
        builder.AppendLine($"Start: Node={_start.Node}, Curr={_start.Curr}, First={_start.First}, Last={_start.Last}");
        builder.AppendLine($"Finish: Node={_finish.Node}, Curr={_finish.Curr}, First={_finish.First}, Last={_finish.Last}");
        foreach (var row in snapshot.Rows)
        {
            builder.Append($"[{row.MapIndex}] -> ");
            if (!row.HasBlock) builder.Append("null");
            foreach (var slot in row.Slots)
            {
                builder.Append('[');
                if (slot.IsStart) builder.Append("S ");
                if (slot.IsFinish) builder.Append("F ");
                builder.Append(slot.IsOccupied ? $"{slot.LogicalIndex}:{slot.Value}" : "empty");
                builder.Append("] ");
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }

    // PushFront용 이동. block의 첫 칸을 넘으면 이전 block의 마지막 칸으로 이동한다.
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

    // PopFront용 이동. 마지막 원소를 꺼낸 경우 이동 후 start와 finish가 같은 위치가 된다.
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

    // PushBack용 이동. block 경계를 넘으면 다음 block의 첫 칸을 새 finish로 확보한다.
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

    // PopBack용 이동. finish가 첫 칸이면 이전 block의 마지막 실제 원소 위치로 돌아간다.
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

        // finish 블록도 보존한다. BlockSize == 1에서는 PushBack 직후의 실제 원소가
        // Curr == 0에 있으므로 이 블록을 제외하면 map 확장 시 값이 유실된다.

        // 크기 1의 map도 앞뒤에 최소 한 노드씩 여유를 둔다.
        MapSize = Math.Max(4, MapSize * 2);
        _map = new T[MapSize][];

        int newStartIdx = Math.Max(1, oldMapSize / 2);
        int oldIdx = oldStartNode;
        int newIdx = newStartIdx;

        // 원소를 개별 복사하지 않고 start부터 finish까지의 block 참조만 새 map으로 옮긴다.
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
