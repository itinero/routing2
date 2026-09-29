using System;

namespace Itinero.Routing.DataStructures;

/// <summary>
/// Implements a priority queue in the form of a binary heap.
/// </summary>
/// <remarks>
/// Priorities and items are separate arrays: sifting is almost all priority comparisons, and a
/// combined (T item, double priority)[] made each one stride over the item to read 8 bytes.
/// </remarks>
internal class BinaryHeap<T>
    where T : struct
{
    private T[] _items;
    private double[] _priorities;
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
        _items = new T[initialSize];
        _priorities = new double[initialSize];
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

        if (_latestIndex == _items.Length - 1)
        {
            Array.Resize(ref _items, _items.Length * 2);
            Array.Resize(ref _priorities, _priorities.Length * 2);
        }

        // ... and let it 'bubble' up using hole-sinking:
        // instead of swapping at each level, leave a hole and move it up.
        var bubbleIndex = _latestIndex;
        _latestIndex++;
        while (bubbleIndex != 1)
        {
            var parentIdx = bubbleIndex / 2;
            if (priority < _priorities[parentIdx])
            {
                _items[bubbleIndex] = _items[parentIdx];
                _priorities[bubbleIndex] = _priorities[parentIdx];
                bubbleIndex = parentIdx;
            }
            else
            {
                break;
            }
        }

        _items[bubbleIndex] = item;
        _priorities[bubbleIndex] = priority;
    }

    /// <summary>
    /// Returns the smallest weight in the queue.
    /// </summary>
    public double PeekWeight()
    {
        return _priorities[1];
    }

    /// <summary>
    /// Returns the object with the smallest weight.
    /// </summary>
    public T Peek()
    {
        return _items[1];
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

        var result = _items[1];
        priority = _priorities[1];

        _count--;
        _latestIndex--;

        // take the last element and sift it down from the root.
        var lastItem = _items[_latestIndex];
        var lastPriority = _priorities[_latestIndex];

        // sift down using hole-sinking: move the smaller child up,
        // place the last element at the final hole position.
        uint hole = 1;
        while (true)
        {
            var child = hole * 2;
            if (child + 1 <= _latestIndex)
            {
                // two children exist — pick the smaller one.
                if (_priorities[child + 1] < _priorities[child])
                {
                    child++;
                }

                if (lastPriority <= _priorities[child])
                {
                    break;
                }

                _items[hole] = _items[child];
                _priorities[hole] = _priorities[child];
                hole = child;
            }
            else if (child <= _latestIndex)
            {
                // only left child exists.
                if (lastPriority <= _priorities[child])
                {
                    break;
                }

                _items[hole] = _items[child];
                _priorities[hole] = _priorities[child];
                hole = child;
            }
            else
            {
                break;
            }
        }

        _items[hole] = lastItem;
        _priorities[hole] = lastPriority;

        return result;
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
