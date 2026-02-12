using System;

namespace Itinero.Network.Tiles.Standalone.Global;

/// <summary>
/// A global edge id.
/// </summary>
public readonly struct GlobalEdgeId : IEquatable<GlobalEdgeId>
{

    private GlobalEdgeId(long edgeId, ushort tail, ushort head)
    {
        this.EdgeId = edgeId;
        this.Tail = tail;
        this.Head = head;
    }

    public static GlobalEdgeId Create(long edgeId, uint tail, uint head)
    {
        if (tail > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(tail));
        if (head > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(head));
        
        return new GlobalEdgeId(edgeId, (ushort)tail, (ushort)head);
    }

    public static GlobalEdgeId Create(long edgeId, int tail, int head)
    {
        if (tail > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(tail));
        if (head > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(head));
        
        return new GlobalEdgeId(edgeId, (ushort)tail, (ushort)head);
    }

    /// <summary>
    /// The edge id, the way id when using OSM.
    /// </summary>
    public long EdgeId { get; }

    /// <summary>
    /// The tail, the index of first node in the way representing the segment when using OSM.
    /// </summary>
    public ushort Tail { get; }

    /// <summary>
    /// The tail, the index of last node in the way representing the segment when using OSM.
    /// </summary>
    public ushort Head { get; }

    /// <summary>
    /// Gets the inverted edge, tail and head switched.
    /// </summary>
    /// <returns></returns>
    public GlobalEdgeId GetInverted()
    {
        return new GlobalEdgeId(this.EdgeId, this.Head, this.Tail);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.EdgeId}_{this.Tail}->{this.Head}";
    }
    
    public bool Equals(GlobalEdgeId other)
    {
        return this.EdgeId == other.EdgeId && this.Tail == other.Tail && this.Head == other.Head;
    }

    public override bool Equals(object? obj)
    {
        return obj is GlobalEdgeId other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.EdgeId, this.Tail, this.Head);
    }

    public static bool operator ==(GlobalEdgeId left, GlobalEdgeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GlobalEdgeId left, GlobalEdgeId right)
    {
        return !left.Equals(right);
    }
}
