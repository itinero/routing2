// using System;
//
// namespace Itinero.Data.Graphs.TurnCosts
// {
//     /// <summary>
//     /// Represents a vertex ID composed of a tile ID and a vertex ID.
//     /// </summary>
//     public readonly struct TurnCostId : IEquatable<TurnCostId>
//     {
//         /// <summary>
//         /// Creates a new vertex id.
//         /// </summary>
//         /// <param name="tileId">The tile id.</param>
//         /// <param name="localId">The local id.</param>
//         public TurnCostId(uint tileId, uint localId)
//         {
//             this.TileId = tileId;
//             this.LocalId = localId;
//         }
//
//         /// <summary>
//         /// Gets or sets the tile id.
//         /// </summary>
//         public uint TileId { get; }
//         
//         /// <summary>
//         /// Gets or sets the local id.
//         /// </summary>
//         public uint LocalId { get; }
//
//         /// <summary>
//         /// Returns an empty vertex id.
//         /// </summary>
//         public static TurnCostId Empty => new TurnCostId(uint.MaxValue, uint.MaxValue);
//
//         /// <summary>
//         /// Returns true if this vertex id is empty.
//         /// </summary>
//         /// <returns></returns>
//         public bool IsEmpty => this.LocalId == uint.MaxValue;
//
//         /// <summary>
//         /// Returns true if this id if local.
//         /// </summary>
//         public bool IsLocal => this.TileId != uint.MaxValue;
//
//         /// <summary>
//         /// Returns true if this id is global.
//         /// </summary>
//         public bool IsGlobal => this.TileId == uint.MaxValue && this.LocalId != uint.MaxValue;
//         
//         /// <summary>
//         /// Returns a human readable description.
//         /// </summary>
//         /// <returns></returns>
//         public override string ToString()
//         {
//             return $"{this.LocalId} @ {this.TileId}";
//         }
//         
//         /// <summary>
//         /// Returns true if the two ids represent the same id.
//         /// </summary>
//         /// <returns></returns>
//         public static bool operator ==(TurnCostId vertex1, TurnCostId vertex2)
//         {
//             return vertex1.LocalId == vertex2.LocalId &&
//                 vertex1.TileId == vertex2.TileId;
//         }
//
//         public static bool operator !=(TurnCostId vertex1, TurnCostId vertex2)
//         {
//             return !(vertex1 == vertex2);
//         }
//
//         public bool Equals(TurnCostId other)
//         {
//             return LocalId == other.LocalId && TileId == other.TileId;
//         }
//
//         public override bool Equals(object obj)
//         {
//             if (ReferenceEquals(null, obj)) return false;
//             return obj is TurnCostId other && Equals(other);
//         }
//
//         public override int GetHashCode()
//         {
//             unchecked
//             {
//                 return ((int) TileId * 397) ^ (int) LocalId;
//             }
//         }
//
//         /// <summary>
//         /// Encodes the info in this id into one 64bit unsigned integer.
//         /// </summary>
//         /// <returns>An encoded version of this vertex.</returns>
//         internal ulong Encode()
//         {
//             return (((ulong) this.TileId) << 32) + this.LocalId;
//         }
//
//         /// <summary>
//         /// Decodes the given encoded id.
//         /// </summary>
//         /// <param name="encoded">The encoded version.</param>
//         /// <param name="tileId">The tile id.</param>
//         /// <param name="localId">The local id.</param>
//         /// <returns>The decoded version.</returns>
//         internal static void Decode(ulong encoded, out uint tileId, out uint localId)
//         {
//             tileId = (uint) (encoded >> 32);
//             var tileOffset = ((ulong) tileId) << 32;
//             localId = (uint) (encoded - tileOffset);
//         }
//
//         /// <summary>
//         /// Decodes the given encoded id.
//         /// </summary>
//         /// <param name="encoded">The encoded version.</param>
//         /// <returns>The decoded version.</returns>
//         internal static TurnCostId Decode(ulong encoded)
//         {
//             Decode(encoded, out var tileId, out var localId);
//             return new TurnCostId(tileId, localId);
//         }
//     }
// }