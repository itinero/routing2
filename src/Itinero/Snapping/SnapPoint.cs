﻿using Itinero.Network;

namespace Itinero.Snapping;

/// <summary>
/// Represents a unique location on an edge in the routing network.
/// </summary>
/// <remarks>
/// The location is defined by the edge id and it's offset as an unsigned 16 bit int:
/// - offset=0 => first vertex of the edge.
/// - offset=X => offset relative to ushort.MaxValue.
/// - offset=ushort.MaxValue => the last vertex of the edge.
/// </remarks>
public readonly struct SnapPoint
{
    /// <summary>
    /// Creates a new snap point.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="offset">The offset.</param>
    public SnapPoint(EdgeId edgeId, ushort offset)
    {
        this.EdgeId = edgeId;
        this.Offset = offset;
    }

    /// <summary>
    /// Gets the edge id.
    /// </summary>
    public EdgeId EdgeId { get; }

    /// <summary>
    /// Gets the offset.
    /// </summary>
    public ushort Offset { get; }

    /// <summary>
    /// Returns true if this snap point is exactly at a vertex location.
    /// </summary>
    public bool IsVertex => this.Offset is 0 or ushort.MaxValue;

    /// <summary>
    /// Gets a description of this snap point.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{this.EdgeId} @ {this.Offset}";
    }
}
