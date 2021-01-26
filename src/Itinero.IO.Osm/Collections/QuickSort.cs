using System;

namespace Itinero.IO.Osm.Collections
{
    internal static class QuickSort
    {
        public static void Sort(Func<long, long> value, Action<long, long> swap, long left, long right)
        {
            Sort((i1, i2) => value(i1).CompareTo(value(i2)), swap, left, right);
        }

        public static void Sort(Func<long, long, int> compare, Action<long, long> swap, long left, long right)
        {
            if (left >= right) {
                return;
            }

            var stack = new System.Collections.Generic.Stack<Pair>();
            stack.Push(new Pair(left, right));
            while (stack.Count > 0) {
                var pair = stack.Pop();
                var pivot = Partition(compare, swap, pair.Left, pair.Right);
                if (pair.Left < pivot) {
                    stack.Push(new Pair(pair.Left, pivot - 1));
                }

                if (pivot < pair.Right) {
                    stack.Push(new Pair(pivot + 1, pair.Right));
                }
            }
        }

        public static bool IsSorted(Func<long, long> value, long left, long right)
        {
            var previous = value(left);
            for (var i = left + 1; i <= right; i++) {
                var val = value(i);
                if (previous > val) {
                    return false;
                }

                previous = val;
            }

            return true;
        }

        private struct Pair
        {
            public Pair(long left, long right)
                : this()
            {
                Left = left;
                Right = right;
            }

            public long Left { get; set; }
            public long Right { get; set; }
        }

        private static long Partition(Func<long, long, int> compare, Action<long, long> swap, long left, long right)
        {
            // get the pivot value.
            if (left > right) {
                throw new ArgumentException("left should be smaller than or equal to right.");
            }

            if (left == right) {
                // sorting just one item results in that item being sorted already and a pivot equal to that item itself.
                return right;
            }

            // select the middle one as the pivot value.
            var pivot = (left + right) / (long) 2;
            if (pivot != left) { // switch.
                swap(pivot, left);
            }

            // start with the left as pivot value.
            pivot = left;
            //var pivotValue = value(pivot);

            while (true) {
                // move the left to the right until the first value bigger than pivot.
                // var leftValue = value(left + 1);
                var leftComparison = compare(left + 1, pivot);
                while (leftComparison <= 0) {
                    left++;
                    if (left == right) {
                        break;
                    }

                    leftComparison = compare(left + 1, pivot);
                }

                // move the right to left until the first value smaller than pivot.
                if (left != right) {
                    // var rightValue = value(right);
                    var rightComparison = compare(right, pivot);
                    while (rightComparison > 0) {
                        right--;
                        if (left == right) {
                            break;
                        }

                        rightComparison = compare(right, pivot);
                    }
                }

                if (left == right) { // we are done searching, left == right.
                    if (pivot != left) { // make sure the pivot value is where it is supposed to be.
                        swap(pivot, left);
                    }

                    return left;
                }

                // switch left<->right.
                swap(left + 1, right);
            }
        }

        public static void ThreewayPartition(Func<long, long> value, Action<long, long> swap, long left, long right,
            out long highestLowest, out long lowestHighest)
        {
            ThreewayPartition(value, swap, left, right, left, out highestLowest,
                out lowestHighest); // default, the left a pivot.
        }

        public static void ThreewayPartition(Func<long, long> value, Action<long, long> swap, long left, long right,
            long pivot,
            out long highestLowest, out long lowestHighest)
        {
            if (left > right) {
                throw new ArgumentException("left should be smaller than or equal to right.");
            }

            if (left == right) {
                // sorting just one item results in that item being sorted already and a pivot equal to that item itself.
                highestLowest = right;
                lowestHighest = right;
                return;
            }

            // get pivot value.
            var pivotValue = value(pivot);

            var i = left;
            var j = left;
            var n = right;

            while (j <= n) {
                var valueJ = value(j);
                if (valueJ < pivotValue) {
                    swap(i, j);
                    i++;
                    j++;
                }
                else if (valueJ > pivotValue) {
                    swap(j, n);
                    n--;
                }
                else {
                    j++;
                }
            }

            highestLowest = i - 1;
            lowestHighest = n + 1;
        }
    }
}