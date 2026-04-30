using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Itinero.Network.Tiles.Standalone.Global;

public static class GlobalRestrictionExtensions
{
    /// <summary>
    /// Tries to build a network restriction from a global restriction.
    /// </summary>
    /// <remarks>
    /// Should always succeed if all edges are available but can fail if the data
    /// available is a tile and does not contain all edges yet.
    /// </remarks>
    /// <param name="globalNetworkRestriction">The global network restriction.</param>
    /// <param name="getEdge">
    /// Resolves a chain edge. The bool argument is <c>true</c> for the first edge
    /// in the chain (anchor with the next edge is at the geid's <see cref="GlobalEdgeId.Head"/>),
    /// <c>false</c> for any subsequent edge (anchor with the previous edge is at
    /// the geid's <see cref="GlobalEdgeId.Tail"/>). The chain-connectivity invariant
    /// emitted by the OSM converters is <c>previous.Head == current.Tail</c> at
    /// the same OSM node, so the anchor end is unambiguous from chain position.
    /// </param>
    /// <param name="networkRestriction">The resulting network restriction, if any.</param>
    /// <returns>True if success, false otherwise.</returns>
    public static bool TryBuildNetworkRestriction(this GlobalRestriction globalNetworkRestriction,
        Func<GlobalEdgeId, bool, (EdgeId edge, bool forward)?> getEdge,
        [MaybeNullWhen(false)] out NetworkRestriction? networkRestriction)
    {
        networkRestriction = null;

        var edges = new List<(EdgeId edge, bool forward)>();
        for (var i = 0; i < globalNetworkRestriction.Count; i++)
        {
            var globalId = globalNetworkRestriction[i];
            var isFirst = i == 0;

            var e = getEdge(globalId, isFirst);
            if (e == null) return false;

            edges.Add(e.Value);
        }

        networkRestriction = new NetworkRestriction(edges, globalNetworkRestriction.IsProhibitory,
            globalNetworkRestriction.Attributes);
        return true;
    }

    /// <summary>
    /// Walks from the chain-anchor end of <paramref name="geid"/> toward the far end,
    /// returning the first sub-edge in the index whose endpoint matches the anchor.
    /// </summary>
    /// <remarks>
    /// Given the OSM converters' invariant that <c>previous.Head == current.Tail</c>
    /// at the shared via node, the anchor end is determined by chain position:
    /// the first edge anchors at <see cref="GlobalEdgeId.Head"/>, every subsequent
    /// edge anchors at <see cref="GlobalEdgeId.Tail"/>. The walk steps one sub-index
    /// at a time, trying both the canonical and inverted lookups; the returned
    /// <c>forward</c> reflects whether the matched stored edge runs in the chain's
    /// traversal direction (Tail → Head of <paramref name="geid"/>).
    /// </remarks>
    public static (EdgeId edge, bool forward)? WalkFromAnchor(GlobalEdgeId geid, bool isFirst,
        TryGetEdgeId tryGet)
    {
        var anchor = isFirst ? geid.Head : geid.Tail;
        var farEnd = isFirst ? geid.Tail : geid.Head;
        if (anchor == farEnd) return null;

        // chainGoesAnchorToFar: chain enters the edge at the anchor and exits at far.
        // True when anchor == geid.Tail (i.e. not the first edge).
        var chainGoesAnchorToFar = !isFirst;

        var step = farEnd > anchor ? 1 : -1;
        for (int otherIdx = anchor + step;
             step > 0 ? otherIdx <= farEnd : otherIdx >= farEnd;
             otherIdx += step)
        {
            var sub = GlobalEdgeId.Create(geid.EdgeId, (ushort)anchor, (ushort)otherIdx);
            if (tryGet(sub, out var eId))
            {
                // canonical direction is anchor → otherIdx (toward far). Forward
                // matches when chain also goes anchor → far.
                return (eId, chainGoesAnchorToFar);
            }
            if (tryGet(sub.GetInverted(), out eId))
            {
                // canonical direction is otherIdx → anchor (toward anchor).
                // Forward matches when chain goes far → anchor.
                return (eId, !chainGoesAnchorToFar);
            }
        }

        return null;
    }

    /// <summary>
    /// Lookup callback for <see cref="WalkFromAnchor"/>: returns whether the
    /// given <see cref="GlobalEdgeId"/> is registered and what
    /// <see cref="EdgeId"/> it maps to.
    /// </summary>
    public delegate bool TryGetEdgeId(GlobalEdgeId geid, out EdgeId edgeId);
}
