using System.Collections.Generic;

namespace Itinero.Data.Graphs.Coders
{
    /// <summary>
    /// The layout of the data on an edge.
    /// </summary>
    public class EdgeDataLayout
    {
        private readonly List<(string key, int offset, EdgeDataType dataType)> _data = new List<(string key, int offset, EdgeDataType dataType)>();
        
        /// <summary>
        /// Adds a new entry.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dataType">The data type.</param>
        /// <returns>The offset the data will be stored at.</returns>
        public int Add(string key, EdgeDataType dataType)
        {
            if (_data.Count == 0)
            {
                _data.Add((key, 0, dataType));
                return 0;
            }

            var offset = this.Size;
            _data.Add((key, offset, dataType));
            return offset;
        }

        /// <summary>
        /// Tries to get the data layout for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="dataLayout">The data layout.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        public bool TryGet(string key, out (int offset, EdgeDataType dataType) dataLayout)
        {
            foreach (var (k, o, dt) in _data)
            {
                if (k != key) continue;
                
                dataLayout = (o, dt);
                return true;
            }

            dataLayout = default;
            return false;
        }

        /// <summary>
        /// The size in bytes this layout takes to store.
        /// </summary>
        public int Size
        {
            get
            {
                if (_data.Count == 0)
                {
                    return 0;
                }
                
                var last = _data[_data.Count - 1];
                return last.offset + last.dataType.ByteCount();
            }
        }
    }
}