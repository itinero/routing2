using System;

namespace Itinero.Data.Graphs
{
    /// <summary>
    /// Represents a edge id composed of a tile id and a local id.
    /// </summary>
    public readonly struct EdgeId : IEquatable<EdgeId>
    {
        /// <summary>
        /// Creates a new vertex id.
        /// </summary>
        /// <param name="tileId">The tile id.</param>
        /// <param name="localId">The local id.</param>
        public EdgeId(uint tileId, uint localId)
        {
            this.TileId = tileId;
            this.LocalId = localId;
        }
        
        /// <summary>
        /// Gets or sets the tile id.
        /// </summary>
        public uint TileId { get; }
        
        /// <summary>
        /// Gets or sets the local id.
        /// </summary>
        public uint LocalId { get; }

        /// <summary>
        /// Returns an empty edge id.
        /// </summary>
        public static readonly EdgeId Empty = new EdgeId(uint.MaxValue, uint.MaxValue);

        /// <summary>
        /// Returns true if this edge id is empty.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return this.TileId == uint.MaxValue;
        }

        /// <summary>
        /// Returns a human readable description.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.LocalId} @ {this.TileId}";
        }
        
        /// <summary>
        /// Returns true if the two edges represent the same id.
        /// </summary>
        /// <returns></returns>
        public static bool operator ==(EdgeId vertex1, EdgeId vertex2)
        {
            return vertex1.LocalId == vertex2.LocalId &&
                vertex1.TileId == vertex2.TileId;
        }
        
        /// <summary>
        /// Returns true if the two edges don't represent the same id.
        /// </summary>
        /// <returns></returns>
        public static bool operator !=(EdgeId vertex1, EdgeId vertex2)
        {
            return !(vertex1 == vertex2);
        }

        /// <summary>
        /// Returns true if the given edge represent the same id.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(EdgeId other)
        {
            return LocalId == other.LocalId && TileId == other.TileId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is EdgeId other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) TileId * 397) ^ (int) LocalId;
            }
        }

        /// <summary>
        /// Encodes the info in this edge into one 64bit unsigned integer.
        /// </summary>
        /// <returns>An encoded version of this edge.</returns>
        internal ulong Encode()
        {
            return (((ulong) this.TileId) << 32) + this.LocalId;
        }

        /// <summary>
        /// Decodes the given encoded edge id.
        /// </summary>
        /// <param name="encoded">The encoded version an edge.</param>
        /// <returns>The decoded version of edge.</returns>
        internal static EdgeId Decode(ulong encoded)
        {
            var tileId = (uint) (encoded >> 32);
            var localId = (uint) (encoded - ((ulong)tileId << 32));
            
            return new EdgeId(tileId, localId);
        }
    }
}