﻿using System;

namespace Itinero.Data.Graphs
{
    /// <summary>
    /// Represents a vertex ID composed of a tile ID and a vertex ID.
    /// </summary>
    public struct VertexId : IEquatable<VertexId>
    {
        /// <summary>
        /// Creates a new vertex id.
        /// </summary>
        /// <param name="tileId">The tile id.</param>
        /// <param name="localId">The local id.</param>
        public VertexId(uint tileId, uint localId)
        {
            this.TileId = tileId;
            this.LocalId = localId;
        }

        /// <summary>
        /// Gets or sets the tile id.
        /// </summary>
        public uint TileId { get;  private set; }
        
        /// <summary>
        /// Gets or sets the local id.
        /// </summary>
        public uint LocalId { get; private set; }

        /// <summary>
        /// Returns an empty vertex id.
        /// </summary>
        public static VertexId Empty => new VertexId()
        {
            LocalId = uint.MaxValue,
            TileId = uint.MaxValue
        };

        /// <summary>
        /// Returns true if this vertex id is empty.
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
        /// Returns true if the two vertices represent the same id.
        /// </summary>
        /// <returns></returns>
        public static bool operator ==(VertexId vertex1, VertexId vertex2)
        {
            return vertex1.LocalId == vertex2.LocalId &&
                vertex1.TileId == vertex2.TileId;
        }

        public static bool operator !=(VertexId vertex1, VertexId vertex2)
        {
            return !(vertex1 == vertex2);
        }

        public bool Equals(VertexId other)
        {
            return LocalId == other.LocalId && TileId == other.TileId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) TileId * 397) ^ (int) LocalId;
            }
        }

        /// <summary>
        /// Encodes the info in this vertex into one 64bit unsigned integer.
        /// </summary>
        /// <returns>An encoded version of this vertex.</returns>
        internal ulong Encode()
        {
            return (((ulong) this.TileId) << 32) + this.LocalId;
        }

        /// <summary>
        /// Decodes the given encoded vertex id.
        /// </summary>
        /// <param name="encoded">The encoded version a vertex.</param>
        /// <param name="tileId">The tile id.</param>
        /// <param name="localId">The local id.</param>
        /// <returns>The decoded version of the vertex.</returns>
        internal static void Decode(ulong encoded, out uint tileId, out uint localId)
        {
            tileId = (uint) (encoded >> 32);
            localId = (uint) (encoded - ((ulong)tileId << 32));
        }

        /// <summary>
        /// Decodes the given encoded vertex id.
        /// </summary>
        /// <param name="encoded">The encoded version a vertex.</param>
        /// <returns>The decoded version of the vertex.</returns>
        internal static VertexId Decode(ulong encoded)
        {
            Decode(encoded, out var tileId, out var localId);
            return new VertexId(tileId, localId);
        }
    }
}