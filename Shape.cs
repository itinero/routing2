using Reminiscence.Arrays;

namespace routing2
{
    /// <summary>
    /// Represents a shape, a sequence of coordinates that represents the shape of an edge.
    /// </summary>
    public class Shape : ShapeBase
    {
        private readonly ArrayBase<float> _coordinates;
        private readonly long _pointer;
        private readonly int _size;
        private readonly bool _reversed;

        /// <summary>
        /// Creates a new shape.
        /// </summary>
        internal Shape(ArrayBase<float> coordinates, long pointer, int size)
        {
            _coordinates = coordinates;
            _pointer = pointer;
            _size = size;
            _reversed = false;
        }

        /// <summary>
        /// Creates a new shape.
        /// </summary>
        internal Shape(ArrayBase<float> coordinates, long pointer, int size, bool reversed)
        {
            _coordinates = coordinates;
            _pointer = pointer;
            _size = size;
            _reversed = reversed;
        }

        /// <summary>
        /// Returns the number of coordinates.
        /// </summary>
        public override int Count
        {
            get { return _size; }
        }

        /// <summary>
        /// Gets the coordinate at the given index.
        /// </summary>
        public override Coordinate this[int i]
        {
            get
            {
                if (_reversed)
                {
                    return new Coordinate()
                    {
                        Latitude = _coordinates[_pointer + ((_size - 1) * 2) - (i * 2)],
                        Longitude = _coordinates[_pointer + ((_size - 1) * 2) - (i * 2) + 1],
                    };
                }
                return new Coordinate()
                {
                    Latitude = _coordinates[_pointer + (i * 2)],
                    Longitude = _coordinates[_pointer + (i * 2) + 1]
                };
            }
        }

        /// <summary>
        /// Returns the same shape but with the order of the coordinates reversed.
        /// </summary>
        public override ShapeBase Reverse()
        {
            return new Shape(_coordinates, _pointer, _size, !_reversed);
        }
    }
}