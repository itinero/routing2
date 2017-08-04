using System.Collections.Generic;

namespace routing2
{
   /// <summary>
    /// Abstract representation of a shape.
    /// </summary>
    public abstract class ShapeBase : IEnumerable<Coordinate>
    {
        /// <summary>
        /// Returns the number of coordinates in this shape.
        /// </summary>
        public abstract int Count
        {
            get;
        }

        /// <summary>
        /// Gets the coordinate at the given index.
        /// </summary>
        public abstract Coordinate this[int i]
        {
            get;
        }

        /// <summary>
        /// Returns the same shape but with the order of the coordinates reversed.
        /// </summary>
        public abstract ShapeBase Reverse();

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        public IEnumerator<Coordinate> GetEnumerator()
        {
            return new ShapeBaseEnumerator(this);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ShapeBaseEnumerator(this);
        }

        private struct ShapeBaseEnumerator : IEnumerator<Coordinate>, System.Collections.IEnumerator
        {
            private readonly ShapeBase _shape;

            public ShapeBaseEnumerator(ShapeBase shape)
            {
                _shape = shape;
                _i = -1;
            }

            private int _i;

            public Coordinate Current
            {
                get { return _shape[_i]; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                _i++;
                return _shape.Count > _i;
            }

            public void Reset()
            {
                _i = -1;
            }

            public void Dispose()
            {

            }
        }
    }
}