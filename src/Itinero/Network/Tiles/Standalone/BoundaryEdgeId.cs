// using System;
//
// namespace Itinero.Network.Tiles.Standalone;
//
// public struct BoundaryEdgeId : IEquatable<BoundaryEdgeId>
// {
//     public BoundaryEdgeId(uint localId)
//     {
//         this.LocalId = localId;
//     }
//
//     public uint LocalId { get; }
//
//     public bool Equals(BoundaryEdgeId other)
//     {
//         return this.LocalId == other.LocalId;
//     }
//
//     public override bool Equals(object? obj)
//     {
//         return obj is BoundaryEdgeId other && this.Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return (int)this.LocalId;
//     }
// }
