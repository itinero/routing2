//using System;
//
//namespace Itinero.Data.Graphs
//{
//    /// <summary>
//    /// Represents a vertex id composed of a tile id and a vertex id.
//    /// </summary>
//    public struct VertexId : IEquatable<VertexId>
//    {
//        /// <summary>
//        /// Creates a new vertex id.
//        /// </summary>
//        /// <param name="tileId">The tile id.</param>
//        /// <param name="localId">The local id.</param>
//        public VertexId(uint tileId, uint localId)
//        {
//            this.TileId = tileId;
//            this.LocalId = localId;
//        }
//        
//        /// <summary>
//        /// Gets or sets the tile id.
//        /// </summary>
//        public uint TileId { get; }
//        
//        /// <summary>
//        /// Gets or sets the local id.
//        /// </summary>
//        public uint LocalId { get; }
//
//        /// <summary>
//        /// Returns an empty vertex id.
//        /// </summary>
//        public static readonly VertexId Empty = new VertexId(uint.MaxValue, uint.MaxValue);
//
//        /// <summary>
//        /// Returns true if this vertex id is empty.
//        /// </summary>
//        /// <returns></returns>
//        public bool IsEmpty()
//        {
//            return this.TileId == uint.MaxValue;
//        }
//
//        /// <summary>
//        /// Returns a human readable description.
//        /// </summary>
//        /// <returns></returns>
//        public override string ToString()
//        {
//            return $"{this.LocalId} @ {this.TileId}";
//        }
//        
//        /// <summary>
//        /// Returns true if the two vertices represent the same id.
//        /// </summary>
//        /// <returns></returns>
//        public static bool operator ==(VertexId vertex1, VertexId vertex2)
//        {
//            return vertex1.LocalId == vertex2.LocalId &&
//                vertex1.TileId == vertex2.TileId;
//        }
//        
//        /// <summary>
//        /// Returns true if the two vertices don't represent the same id.
//        /// </summary>
//        /// <returns></returns>
//        public static bool operator !=(VertexId vertex1, VertexId vertex2)
//        {
//            return !(vertex1 == vertex2);
//        }
//
//        /// <summary>
//        /// Returns true if the given vertex represent the same id.
//        /// </summary>
//        /// <param name="other"></param>
//        /// <returns></returns>
//        public bool Equals(VertexId other)
//        {
//            return LocalId == other.LocalId && TileId == other.TileId;
//        }
//
//        /// <inheritdoc/>
//        public override bool Equals(object obj)
//        {
//            return obj is VertexId other && Equals(other);
//        }
//
//        /// <inheritdoc/>
//        public override int GetHashCode()
//        {
//            unchecked
//            {
//                return ((int) TileId * 397) ^ (int) LocalId;
//            }
//        }
//    }
//}