using System.Collections.Generic;

public class Heap<T>
{
    private const int WrongIndex = -1;

    public int Count { get; private set; }
    public int LastStepCompareIndex { get; private set; } = WrongIndex;

    private List<T> _vector = new List<T>();

    public void Initialize()
    {
        _vector.Clear();
        Count = 0;
        LastStepCompareIndex = WrongIndex;
    }

    public void Push(T value)
    {
        _vector.Add(value);
        Count++;

        SiftUp(Count - 1);
    }

    public bool Pop(out T value)
    {
        value = default(T);

        if (IsEmpty())
            return false;

        value = _vector[0];

        int lastIndex = Count - 1;
        T lastValue = _vector[lastIndex];

        _vector.RemoveAt(lastIndex);
        Count--;

        // Sift Down 할 필요가 없다면 return.
        if (Count == 0)
            return true;

        // 마지막 원소를 top에 가져온 뒤, sift down (log(n)) 진행
        _vector[0] = lastValue;

        SiftDown(0);
        return true;
    }

    public int BeginPushStep(T value)
    {
        _vector.Add(value);
        Count++;
        LastStepCompareIndex = WrongIndex;

        return Count - 1;
    }

    public int BeginPopStep(out T value)
    {
        value = default;
        LastStepCompareIndex = WrongIndex;

        if (IsEmpty())
            return WrongIndex;

        value = _vector[0];

        int lastIndex = Count - 1;
        T lastValue = _vector[lastIndex];

        _vector.RemoveAt(lastIndex);
        Count--;

        if (Count == 0)
            return WrongIndex;

        _vector[0] = lastValue;

        return 0;
    }

    public void Clear()
    {
        _vector.Clear();
        Count = 0;
        LastStepCompareIndex = WrongIndex;
    }

    public bool IsEmpty()
    {
        return Count <= 0;
    }

    public T GetValueAt(int index)
    {
        return _vector[index];
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = GetParentIndex(index);

            // 부모가 나보다 크다면 멈추기
            if (Comparer<T>.Default.Compare(_vector[parentIndex], _vector[index]) >= 0)
                break;

            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int leftChildIndex = GetLeftChildIndex(index);
            if (leftChildIndex >= Count)
                break;

            int rightChildIndex = GetRightChildIndex(index);

            // left와 right 크기 비교
            int largerChildIndex = leftChildIndex;
            if (rightChildIndex < Count &&
                Comparer<T>.Default.Compare(_vector[rightChildIndex], _vector[leftChildIndex]) > 0)
            {
                largerChildIndex = rightChildIndex;
            }

            // 두 자식보다 큰 곳이 부모로 있어도 되는 자리이다.
            if (Comparer<T>.Default.Compare(_vector[index], _vector[largerChildIndex]) >= 0)
                break;

            Swap(index, largerChildIndex);
            index = largerChildIndex;
        }
    }

    public int SiftUpStep(int index, bool swap = true)
    {
        LastStepCompareIndex = WrongIndex;

        if (index <= 0)
            return WrongIndex;

        int parentIndex = GetParentIndex(index);
        LastStepCompareIndex = parentIndex;

        // 부모가 나보다 크다면 멈추기
        if (Comparer<T>.Default.Compare(_vector[parentIndex], _vector[index]) >= 0)
            return WrongIndex;

        if (swap)
            Swap(parentIndex, index);

        return parentIndex;
    }

    public int SiftDownStep(int index, bool swap = true)
    {
        LastStepCompareIndex = WrongIndex;

        int leftChildIndex = GetLeftChildIndex(index);
        if (leftChildIndex >= Count)
            return WrongIndex;

        int rightChildIndex = GetRightChildIndex(index);

        // left와 right 크기 비교
        int largerChildIndex = leftChildIndex;
        if (rightChildIndex < Count &&
            Comparer<T>.Default.Compare(_vector[rightChildIndex], _vector[leftChildIndex]) > 0)
        {
            largerChildIndex = rightChildIndex;
        }
        LastStepCompareIndex = largerChildIndex;

        // 두 자식보다 큰 곳이 부모로 있어도 되는 자리이다.
        if (Comparer<T>.Default.Compare(_vector[index], _vector[largerChildIndex]) >= 0)
            return WrongIndex;

        if (swap)
            Swap(index, largerChildIndex);

        return largerChildIndex;
    }

    private void Swap(int firstIndex, int secondIndex)
    {
        T tmp = _vector[firstIndex];
        _vector[firstIndex] = _vector[secondIndex];
        _vector[secondIndex] = tmp;
    }

    private int GetParentIndex(int index)
    {
        // (index - 1)로 형제 (1,2), (3,4), (5,6)를 같은 묶음으로 만든 뒤,
        // 정수 나눗셈 / 2로 해당 묶음의 부모 인덱스를 구한다.
        return (index - 1) / 2;
    }

    private int GetLeftChildIndex(int index)
    {
        // 앞선 부모들의 자식 슬롯이 두 칸씩 배정되므로 왼쪽 자식은 2 * index + 1이다.
        return index * 2 + 1;
    }

    private int GetRightChildIndex(int index)
    {
        // 왼쪽 자식 다음 칸이 오른쪽 자식이므로 2 * index + 2이다.
        return index * 2 + 2;
    }
}
