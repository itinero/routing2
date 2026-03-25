using System;

namespace Itinero.Routing.DataStructures;

/// <summary>
/// Implements a priority queue in the form of a binary heap.
/// </summary>
internal class BinaryHeap<T>
    where T : struct
{
    private (T item, double priority)[] _data;
    private int _count;
    private uint _latestIndex;

    /// <summary>
    /// Creates a new binary heap.
    /// </summary>
    public BinaryHeap()
        : this(1024) { }

    /// <summary>
    /// Creates a new binary heap.
    /// </summary>
    public BinaryHeap(uint initialSize)
    {
        _data = new (T, double)[initialSize];
        _count = 0;
        _latestIndex = 1;
    }

    /// <summary>
    /// Returns the number of items in this queue.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Enqueues a given item.
    /// </summary>
    public void Push(T item, double priority)
    {
        _count++;

        if (_latestIndex == _data.Length - 1)
        {
            Array.Resize(ref _data, _data.Length * 2);
        }

        // add the item at the first free point.
        _data[_latestIndex] = (item, priority);

        // ... and let it 'bubble' up using hole-sinking:
        // instead of swapping at each level, leave a hole and move it up.
        var bubbleIndex = _latestIndex;
        _latestIndex++;
        while (bubbleIndex != 1)
        {
            var parentIdx = bubbleIndex / 2;
            if (priority < _data[parentIdx].priority)
            {
                _data[bubbleIndex] = _data[parentIdx];
                bubbleIndex = parentIdx;
            }
            else
            {
                break;
            }
        }

        _data[bubbleIndex] = (item, priority);
    }

    /// <summary>
    /// Returns the smallest weight in the queue.
    /// </summary>
    public double PeekWeight()
    {
        return _data[1].priority;
    }

    /// <summary>
    /// Returns the object with the smallest weight.
    /// </summary>
    public T Peek()
    {
        return _data[1].item;
    }

    /// <summary>
    /// Returns the object with the smallest weight and removes it.
    /// </summary>
    public T Pop(out double priority)
    {
        priority = 0;
        if (_count <= 0)
        {
            return default;
        }

        var result = _data[1];
        priority = result.priority;

        _count--;
        _latestIndex--;

        // take the last element and sift it down from the root.
        var last = _data[_latestIndex];
        var lastPriority = last.priority;

        // sift down using hole-sinking: move the smaller child up,
        // place the last element at the final hole position.
        uint hole = 1;
        while (true)
        {
            var child = hole * 2;
            if (child + 1 <= _latestIndex)
            {
                // two children exist — pick the smaller one.
                if (_data[child + 1].priority < _data[child].priority)
                {
                    child++;
                }

                if (lastPriority <= _data[child].priority)
                {
                    break;
                }

                _data[hole] = _data[child];
                hole = child;
            }
            else if (child <= _latestIndex)
            {
                // only left child exists.
                if (lastPriority <= _data[child].priority)
                {
                    break;
                }

                _data[hole] = _data[child];
                hole = child;
            }
            else
            {
                break;
            }
        }

        _data[hole] = last;

        return result.item;
    }

    /// <summary>
    /// Clears this priority queue.
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _latestIndex = 1;
    }
}
