using System;

namespace Itinero.Algorithms.DataStructures
{
    /// <summary>
    /// Implements a priority queue in the form of a binary heap.
    /// </summary>
    internal class BinaryHeap<T>
        where T : struct
    {
        private T[] _heap; // The objects per priority.
        private float[] _priorities; // Holds the priorities of this heap.
        private int _count; // The current count of elements.
        private uint _latestIndex; // The latest unused index

        /// <summary>
        /// Creates a new binary heap.
        /// </summary>
        public BinaryHeap()
            : this(2)
        {

        }

        /// <summary>
        /// Creates a new binary heap.
        /// </summary>
        public BinaryHeap(uint initialSize)
        {
            _heap = new T[initialSize];
            _priorities = new float[initialSize];

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
        public void Push(T item, float priority)
        {
            _count++; // another item was added!

            // increase size if needed.
            if (_latestIndex == _priorities.Length - 1)
            {
                // time to increase size!
                Array.Resize<T>(ref _heap, _heap.Length + 100);
                Array.Resize<float>(ref _priorities, _priorities.Length + 100);
            }

            // add the item at the first free point 
            _priorities[_latestIndex] = priority;
            _heap[_latestIndex] = item;

            // ... and let it 'bubble' up.
            var bubbleIndex = _latestIndex;
            _latestIndex++;
            while (bubbleIndex != 1)
            {
                // bubble until the indx is one.
                var parentIdx = bubbleIndex / 2;
                if (_priorities[bubbleIndex] < _priorities[parentIdx])
                {
                    // the parent priority is higher; do the swap.
                    var tempPriority = _priorities[parentIdx];
                    var tempItem = _heap[parentIdx];
                    _priorities[parentIdx] = _priorities[bubbleIndex];
                    _heap[parentIdx] = _heap[bubbleIndex];
                    _priorities[bubbleIndex] = tempPriority;
                    _heap[bubbleIndex] = tempItem;

                    bubbleIndex = parentIdx;
                }
                else
                {
                    // the parent priority is lower or equal; the item will not bubble up more.
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the smallest weight in the queue.
        /// </summary>
        public float PeekWeight()
        {
            return _priorities[1];
        }

        /// <summary>
        /// Returns the object with the smallest weight.
        /// </summary>
        public T Peek()
        {
            return _heap[1];
        }

        /// <summary>
        /// Returns the object with the smallest weight and removes it.
        /// </summary>
        public T Pop(out float priority)
        {
            priority = 0;
            if (_count <= 0) return default(T);

            var item = _heap[1]; // get the first item.
            priority = _priorities[1];

            _count--; // reduce the element count.
            _latestIndex--; // reduce the latest index.

            int swapitem = 1, parent = 1;
            var parentPriority = _priorities[_latestIndex];
            var parentItem = _heap[_latestIndex];
            _heap[1] = parentItem; // place the last element on top.
            _priorities[1] = parentPriority; // place the last element on top.
            do
            {
                parent = swapitem;
                var swapItemPriority = 0f;
                if ((2 * parent + 1) <= _latestIndex)
                {
                    swapItemPriority = _priorities[2 * parent];
                    var potentialSwapItem = _priorities[2 * parent + 1];
                    if (parentPriority >= swapItemPriority)
                    {
                        swapitem = 2 * parent;
                        if (_priorities[swapitem] >= potentialSwapItem)
                        {
                            swapItemPriority = potentialSwapItem;
                            swapitem = 2 * parent + 1;
                        }
                    }
                    else if (parentPriority >= potentialSwapItem)
                    {
                        swapItemPriority = potentialSwapItem;
                        swapitem = 2 * parent + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else if ((2 * parent) <= _latestIndex)
                {
                    // Only one child exists
                    swapItemPriority = _priorities[2 * parent];
                    if (parentPriority >= swapItemPriority)
                    {
                        swapitem = 2 * parent;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

                _priorities[parent] = swapItemPriority;
                _priorities[swapitem] = parentPriority;
                _heap[parent] = _heap[swapitem];
                _heap[swapitem] = parentItem;

            } while (true);

            return item;
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
}