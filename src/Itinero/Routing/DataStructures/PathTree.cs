namespace Itinero.Routing.DataStructures {
    /// <summary>
    /// Represents a tree of paths by linking their segments together.
    /// </summary>
    internal sealed class PathTree {
        // TODO: this entire path tree idea can be faster by storing raw structs.

        /// <summary>
        /// Creates a new path tree.
        /// </summary>
        public PathTree() {
            // TODO: perhaps we can reuse arrays somehow.
            _data = new uint[1024];
        }

        private uint[] _data;
        private uint _pointer;

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1) {
            var id = _pointer;
            if (_data.Length <= _pointer + 2) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _pointer += 2;
            return id;
        }

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1, uint data2) {
            var id = _pointer;
            if (_data.Length <= _pointer + 3) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _data[_pointer + 2] = data2;
            _pointer += 3;
            return id;
        }

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1, uint data2, uint data3) {
            var id = _pointer;
            if (_data.Length <= _pointer + 4) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _data[_pointer + 2] = data2;
            _data[_pointer + 3] = data3;
            _pointer += 4;
            return id;
        }

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1, uint data2, uint data3, uint data4) {
            var id = _pointer;
            if (_data.Length <= _pointer + 5) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _data[_pointer + 2] = data2;
            _data[_pointer + 3] = data3;
            _data[_pointer + 4] = data4;
            _pointer += 5;
            return id;
        }

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1, uint data2, uint data3, uint data4, uint data5) {
            var id = _pointer;
            if (_data.Length <= _pointer + 6) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _data[_pointer + 2] = data2;
            _data[_pointer + 3] = data3;
            _data[_pointer + 4] = data4;
            _data[_pointer + 5] = data5;
            _pointer += 6;
            return id;
        }

        /// <summary>
        /// Adds a new segment.
        /// </summary>
        /// <returns></returns>
        public uint Add(uint data0, uint data1, uint data2, uint data3, uint data4, uint data5, uint data6) {
            var id = _pointer;
            if (_data.Length <= _pointer + 7) {
                System.Array.Resize(ref _data, _data.Length * 2);
            }

            _data[_pointer + 0] = data0;
            _data[_pointer + 1] = data1;
            _data[_pointer + 2] = data2;
            _data[_pointer + 3] = data3;
            _data[_pointer + 4] = data4;
            _data[_pointer + 5] = data5;
            _data[_pointer + 6] = data6;
            _pointer += 7;
            return id;
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1, out uint data2) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
            data2 = _data[pointer + 2];
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1, out uint data2,
            out uint data3) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
            data2 = _data[pointer + 2];
            data3 = _data[pointer + 3];
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1, out uint data2,
            out uint data3, out uint data4) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
            data2 = _data[pointer + 2];
            data3 = _data[pointer + 3];
            data4 = _data[pointer + 4];
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1, out uint data2,
            out uint data3, out uint data4, out uint data5) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
            data2 = _data[pointer + 2];
            data3 = _data[pointer + 3];
            data4 = _data[pointer + 4];
            data5 = _data[pointer + 5];
        }

        /// <summary>
        /// Gets the data at the given pointer.
        /// </summary>
        public void Get(uint pointer, out uint data0, out uint data1, out uint data2,
            out uint data3, out uint data4, out uint data5, out uint data6) {
            data0 = _data[pointer + 0];
            data1 = _data[pointer + 1];
            data2 = _data[pointer + 2];
            data3 = _data[pointer + 3];
            data4 = _data[pointer + 4];
            data5 = _data[pointer + 5];
            data6 = _data[pointer + 6];
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear() {
            _pointer = 0;
        }
    }
}