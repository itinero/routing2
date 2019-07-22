using System;
using System.Collections.Generic;
using System.IO;

namespace Itinero.Data.Graphs.Coders
{
    // TODO: before release, make sure this is internalized.
    
    /// <summary>
    /// The layout of the data on an edge.
    /// </summary>
    public class EdgeDataLayout
    {
        private readonly List<(string key, int offset, EdgeDataType dataType)> _data = new List<(string key, int offset, EdgeDataType dataType)>();

        /// <summary>
        /// Creates a new layout.
        /// </summary>
        /// <param name="layouts">The initial layouts.</param>
        public EdgeDataLayout(IEnumerable<(string key, EdgeDataType dataType)> layouts = null)
        {
            if (layouts == null) return;
            
            foreach (var (key, dataType) in layouts)
            {
                this.Add(key, dataType);
            }
        }
        
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
            
            // check for already existing keys.
            foreach (var (existingKey, _, _) in _data)
            {
                if (existingKey == key) throw new ArgumentException($"Cannot add two layouts with the same key.");
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

        internal static EdgeDataLayout ReadFrom(Stream stream)
        {
            // read & verify header.
            var header = stream.ReadWithSizeString();
            var version = stream.ReadByte();
            if (header != nameof(EdgeDataLayout)) throw new InvalidDataException($"Cannot read {nameof(EdgeDataLayout)}: Header invalid.");
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(EdgeDataLayout)}: Version # invalid.");
            
            // read size.
            var size = stream.ReadInt64();
            
            // read layouts.
            var edgeDataLayout = new EdgeDataLayout();
            for (var l = 0; l < size; l++)
            {
                var key = stream.ReadWithSizeString();
                var offset = stream.ReadInt32();
                var dataType = EdgeDataTypeExtensions.FromByte((byte)stream.ReadByte());
                
                edgeDataLayout._data.Add((key, offset, dataType));
            }

            return edgeDataLayout;
        }

        internal long WriteTo(Stream stream)
        {
            // write header (name + version).
            var p = stream.Position;
            stream.WriteWithSize(nameof(EdgeDataLayout));
            stream.WriteByte(1); // version 1.
            
            // write size.
            var bytes = BitConverter.GetBytes((long) _data.Count);
            stream.Write(bytes, 0, 8);

            // write layouts.
            for (var i = 0; i < _data.Count; i++)
            {
                var edgeLayout = _data[i];
                stream.WriteWithSize(edgeLayout.key);
                bytes = BitConverter.GetBytes(edgeLayout.offset);
                stream.Write(bytes, 0, 4);
                stream.WriteByte(edgeLayout.dataType.ToByte());
            }

            return stream.Position - p;
        }
    }
}