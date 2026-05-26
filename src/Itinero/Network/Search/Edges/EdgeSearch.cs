using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Snapping;

namespace Itinero.Network.Search.Edges;

internal static class EdgeSearch
{
    /// <summary>
    /// Returns the closest edge to the center of the given box that has at least one vertex inside the given box.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="searchBox">The box to search in.</param>
    /// <param name="maxDistance">The maximum distance of any snap point returned relative to the center of the search box.</param>
    /// <param name="edgeChecker">Used to determine if an edge is acceptable or not. If null any edge will be accepted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The closest edge to the center of the box inside the given box.</returns>
    public static async Task<SnapPoint> SnapInBoxAsync(this RoutingNetwork network,
        ((double longitude, double, float? e) topLeft, (double longitude, double latitude, float? e) bottomRight)
            searchBox,
        IEdgeChecker? edgeChecker = null, double maxDistance = double.MaxValue, CancellationToken cancellationToken = default)
    {
        var center = ((double longitude, double latitude, float? e))searchBox.Center();
        var zoom = network.Zoom;

        const double exactTolerance = 1;
        var bestDistance = maxDistance;
        (EdgeId edgeId, ushort offset) bestSnapPoint = (EdgeId.Empty, ushort.MaxValue);

        // Spiral tile iteration: start at the tile containing Q, expand to
        // ring 1 (8 neighbours), ring 2, ..., until rings fall outside the
        // search box. Closest-first ordering means the first snap lands fast
        // and bestDistance shrinks early, so outer-ring tiles get rejected
        // by the per-tile predicate before we ever read their edges. Diff
        // computation is lazy per-tile, so cold-path snaps avoid touching
        // tiles the inner-ring snap already made irrelevant.

        // Search box tile-range bounds (clamped, can be a single tile). The
        // searchBox.topLeft's latitude field is unnamed in the parameter
        // signature (legacy quirk), so we access it positionally as Item2.
        var tlTile = TileStatic.WorldToTile(searchBox.topLeft.longitude, searchBox.topLeft.Item2, zoom);
        var brTile = TileStatic.WorldToTile(searchBox.bottomRight.longitude, searchBox.bottomRight.latitude, zoom);
        var minX = tlTile.x; var maxX = brTile.x;
        var minY = tlTile.y; var maxY = brTile.y;

        // Start tile = Q's tile. Outermost ring needed = max distance from
        // start to any corner of the box, in tile units.
        var centerTile = TileStatic.WorldToTile(center.longitude, center.latitude, zoom);
        var cx = centerTile.x;
        var cy = centerTile.y;
        var maxRing = (uint)Math.Max(
            Math.Max(SafeDelta(cx, minX), SafeDelta(maxX, cx)),
            Math.Max(SafeDelta(cy, minY), SafeDelta(maxY, cy)));

        var edgeEnumerator = network.GetEdgeEnumerator();

        for (uint ring = 0; ring <= maxRing; ring++)
        {
            if (bestDistance <= 0) break;

            foreach (var (x, y) in TilesInRing(cx, cy, ring))
            {
                if (bestDistance <= 0) break;
                if (x < minX || x > maxX || y < minY || y > maxY) continue;

                var tileId = TileStatic.ToLocalId(x, y, zoom);
                var tile = network.GetTileForRead(tileId);
                if (tile == null) continue;
                tile.EnsureDiffs(network);  // lazy — only for tiles we actually visit
                var bbox = TileStatic.GetTileBoundingBox(zoom, tileId);
                if (!TileCanReach(bbox, tile.MaxLonDiff, tile.MaxLatDiff, center, bestDistance)) continue;

                var tileMaxLonDiff = tile.MaxLonDiff;
                var tileMaxLatDiff = tile.MaxLatDiff;

                for (uint v = 0; v < tile.VertexCount; v++)
                {
                    if (bestDistance <= 0) break;
                    var vertexId = new VertexId(tileId, v);
                    if (!network.TryGetVertex(vertexId, out var vLon, out var vLat, out var vE)) continue;

                    // Restore the PR1 candidate set: vertices inside the search
                    // box. Without this, we'd walk every vertex in every tile
                    // overlapping the box — including those geometrically outside
                    // the box. Under bestDistance = ∞ (initial state with
                    // MaxDistance = ∞) the VertexCanReach predicate is a no-op,
                    // so without this check we visit ~40% more vertices than PR1
                    // did, paying the per-edge bbox iteration cost for each.
                    if (!searchBox.Overlaps((vLon, vLat, vE))) continue;

                    // Per-vertex predicate: an edge from this vertex extends at
                    // most (tileMaxLonDiff, tileMaxLatDiff) in each axis, so if
                    // the vertex itself is too far for even that envelope to
                    // reach the current best, every edge from it is irrelevant.
                    if (!VertexCanReach(vLon, vLat, tileMaxLonDiff, tileMaxLatDiff, center, bestDistance)) continue;

                    if (!edgeEnumerator.MoveTo(vertexId)) continue;

                    while (edgeEnumerator.MoveNext())
                    {
                        if (bestDistance <= 0) break;

                        // PR1: per-edge MBR prefilter. After the tile and vertex
                        // predicates above, only edges that *might* beat the
                        // current best reach this point — but the bbox prefilter
                        // is still useful to skip edges whose tight bbox can't
                        // beat best even though their vertex passed the looser
                        // (per-tile-extent) check.
                        if (!EdgeBboxCanBeat(edgeEnumerator, center, bestDistance)) continue;

                        // search for the local snap point that improves the current best snap point.
                        (EdgeId edgeId, double offset) localSnapPoint = (EdgeId.Empty, 0);
                        var isAcceptable = edgeChecker == null ? (bool?)true : null;
                        var completeShape = edgeEnumerator.GetCompleteShape();
                        var length = 0.0;
                        using (var completeShapeEnumerator = completeShape.GetEnumerator())
                        {
                            completeShapeEnumerator.MoveNext();
                            var previous = completeShapeEnumerator.Current;

                            // start with the first location.
                            var distance = previous.DistanceEstimateInMeter(center);
                            if (distance < bestDistance)
                            {
                                isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ??
                                                                          await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                                if (!isAcceptable.Value)
                                {
                                    continue;
                                }

                                if (distance < exactTolerance)
                                {
                                    distance = 0;
                                }

                                bestDistance = distance;
                                localSnapPoint = (edgeEnumerator.EdgeId, 0);
                            }

                            // loop over all pairs.
                            while (completeShapeEnumerator.MoveNext())
                            {
                                var current = completeShapeEnumerator.Current;

                                var segmentLength = previous.DistanceEstimateInMeter(current);

                                // first check the actual current location, it may be an exact match.
                                distance = current.DistanceEstimateInMeter(center);
                                if (distance < bestDistance)
                                {
                                    isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ??
                                                     await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                                    if (!isAcceptable.Value)
                                    {
                                        break;
                                    }

                                    if (distance < exactTolerance)
                                    {
                                        distance = 0;
                                    }

                                    bestDistance = distance;
                                    localSnapPoint = (edgeEnumerator.EdgeId, length + segmentLength);
                                }

                                // update length.
                                var startLength = length;
                                length += segmentLength;

                                // TODO: figure this out, there has to be a way to not project every segment.
                                //                        // check if we even need to check.
                                //                        var previousDistance = previous.DistanceEstimateInMeter(center);
                                //                        var shapePointDistance = current.DistanceEstimateInMeter(center);
                                //                        if (previousDistance + segmentLength > bestDistance &&
                                //                            shapePointDistance + segmentLength > bestDistance)
                                //                        {
                                //                            continue;
                                //                        }

                                // project on line segment.
                                var line = (previous, current);
                                var originalPrevious = previous;
                                previous = current;
                                if (bestDistance <= 0)
                                {
                                    // we need to continue, we need the total length.
                                    continue;
                                }

                                var projected = line.ProjectOn(center);
                                if (!projected.HasValue)
                                {
                                    continue;
                                }

                                distance = projected.Value.DistanceEstimateInMeter(center);
                                if (!(distance < bestDistance))
                                {
                                    continue;
                                }

                                isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ??
                                                 await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                                if (!isAcceptable.Value)
                                {
                                    break;
                                }

                                if (distance < exactTolerance)
                                {
                                    distance = 0;
                                }

                                bestDistance = distance;
                                localSnapPoint = (edgeEnumerator.EdgeId,
                                    startLength + originalPrevious.DistanceEstimateInMeter(projected.Value));
                            }
                        }

                        // move to the nex edge if no better point was found.
                        if (localSnapPoint.edgeId == EdgeId.Empty)
                        {
                            continue;
                        }

                        // calculate the actual offset.
                        var offset = ushort.MaxValue;
                        if (localSnapPoint.offset < length)
                        {
                            if (localSnapPoint.offset <= 0)
                            {
                                offset = 0;
                            }
                            else
                            {
                                offset = (ushort)(localSnapPoint.offset / length * ushort.MaxValue);
                            }
                        }

                        // invert offset if edge is reversed.
                        if (!edgeEnumerator.Forward)
                        {
                            offset = (ushort)(ushort.MaxValue - offset);
                        }

                        bestSnapPoint = (localSnapPoint.edgeId, offset);
                    } // while (edgeEnumerator.MoveNext())
                } // for (uint v = 0; v < tile.VertexCount; v++)
            } // foreach (var (x, y) in TilesInRing(...))
        } // for (uint ring = 0; ring <= maxRing; ring++)

        return new SnapPoint(bestSnapPoint.edgeId, bestSnapPoint.offset);
    }

    /// <summary>
    /// Snaps all points in the given box that could potentially be snapping points.
    /// </summary>
    /// <param name="routerDb"></param>
    /// <param name="searchBox">The box to search in.</param>
    /// <param name="maxDistance">The maximum distance of any snap point returned relative to the center of the search box.</param>
    /// <param name="edgeChecker">Used to determine if an edge is acceptable or not. If null any edge will be accepted.</param>
    /// <param name="nonOrthogonalEdges">When true the best potential location on each edge is returned, when false only orthogonal projected points.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All edges that could potentially be relevant snapping points, not only the closest.</returns>
    public static async IAsyncEnumerable<SnapPoint> SnapAllInBoxAsync(this RoutingNetwork routerDb,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) searchBox,
        IEdgeChecker? edgeChecker = null, bool nonOrthogonalEdges = true, double maxDistance = double.MaxValue, CancellationToken cancellationToken = default)
    {
        var edges = new HashSet<EdgeId>();
        var center = ((double longitude, double latitude, float? e))searchBox.Center();
        var zoom = routerDb.Zoom;

        // Tile-bounded iteration — same structure as SnapInBoxAsync but with
        // a fixed maxDistance cutoff (no shrinking-best). Predicates trim
        // tiles and vertices that can't possibly contribute any candidate.
        // Under unbounded maxDistance they degenerate to "always true" and
        // we fall back to the per-edge prefilter alone — same cost as before.
        var candidates = new List<(uint tileId, NetworkTile tile)>();
        foreach (var (x, y) in searchBox.TileRange(zoom))
        {
            var tileId = TileStatic.ToLocalId(x, y, zoom);
            var tile = routerDb.GetTileForRead(tileId);
            if (tile == null) continue;
            tile.EnsureDiffs(routerDb);  // resolve cross-tile edge endpoints accurately
            var bbox = TileStatic.GetTileBoundingBox(zoom, tileId);
            if (!TileCanReach(bbox, tile.MaxLonDiff, tile.MaxLatDiff, center, maxDistance)) continue;
            candidates.Add((tileId, tile));
        }

        var edgeEnumerator = routerDb.GetEdgeEnumerator();
        foreach (var (tileId, tile) in candidates)
        {
            var tileMaxLonDiff = tile.MaxLonDiff;
            var tileMaxLatDiff = tile.MaxLatDiff;

            for (uint v = 0; v < tile.VertexCount; v++)
            {
                var vertexId = new VertexId(tileId, v);
                if (!routerDb.TryGetVertex(vertexId, out var vLon, out var vLat, out var vE)) continue;

                // Preserve the historical contract: candidates come from
                // vertices inside the search box. Under maxDistance = ∞ the
                // VertexCanReach predicate is a no-op, so without this check
                // we'd iterate every vertex in every overlapping tile —
                // including those geometrically outside the box — and
                // measurably regress ToAllAsync_Drain.
                if (!searchBox.Overlaps((vLon, vLat, vE))) continue;

                if (!VertexCanReach(vLon, vLat, tileMaxLonDiff, tileMaxLatDiff, center, maxDistance)) continue;

                if (!edgeEnumerator.MoveTo(vertexId)) continue;
                while (edgeEnumerator.MoveNext())
                {
                    if (!edges.Add(edgeEnumerator.EdgeId)) continue;

                    // PR1 per-edge prefilter — same idea as SnapInBoxAsync.
                    // Under maxDistance = ∞ this also degenerates, leaving
                    // SnapAllInBoxAsync at same cost as before this PR.
                    if (!EdgeBboxCanBeat(edgeEnumerator, center, maxDistance)) continue;

                    // search for the best snap point for the current edge.
                    (EdgeId edgeId, double offset, bool isOrthoganal, double distance) bestEdgeSnapPoint =
                        (EdgeId.Empty, 0, false, maxDistance);
                    var isAcceptable = edgeChecker == null ? (bool?)true : null;
                    var completeShape = edgeEnumerator.GetCompleteShape();
                    var length = 0.0;
                    using (var completeShapeEnumerator = completeShape.GetEnumerator())
                    {
                        completeShapeEnumerator.MoveNext();
                        var previous = completeShapeEnumerator.Current;

                        // start with the first location.
                        var distance = previous.DistanceEstimateInMeter(center);
                        if (distance < bestEdgeSnapPoint.distance)
                        {
                            isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ?? await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                            if (!isAcceptable.Value)
                            {
                                continue;
                            }

                            bestEdgeSnapPoint = (edgeEnumerator.EdgeId, 0, false, distance);
                        }

                        // loop over all pairs.
                        while (completeShapeEnumerator.MoveNext())
                        {
                            var current = completeShapeEnumerator.Current;

                            var segmentLength = previous.DistanceEstimateInMeter(current);

                            // first check the actual current location, it may be an exact match.
                            distance = current.DistanceEstimateInMeter(center);
                            if (distance < bestEdgeSnapPoint.distance)
                            {
                                isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ?? await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                                if (!isAcceptable.Value)
                                {
                                    break;
                                }

                                bestEdgeSnapPoint = (edgeEnumerator.EdgeId, length + segmentLength, false, distance);
                            }

                            // update length.
                            var startLength = length;
                            length += segmentLength;

                            // project on line segment.
                            var line = (previous, current);
                            var originalPrevious = previous;
                            previous = current;

                            var projected = line.ProjectOn(center);
                            if (projected.HasValue)
                            {
                                distance = projected.Value.DistanceEstimateInMeter(center);
                                if (distance < bestEdgeSnapPoint.distance)
                                {
                                    isAcceptable ??= edgeChecker!.IsAcceptable(edgeEnumerator) ?? await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken);
                                    if (isAcceptable.Value)
                                    {
                                        bestEdgeSnapPoint = (edgeEnumerator.EdgeId,
                                            startLength + originalPrevious.DistanceEstimateInMeter(projected.Value),
                                            true, distance);
                                    }
                                }
                            }
                        }
                    }

                    // move to the nex edge if no snap point was found.
                    if (bestEdgeSnapPoint.edgeId == EdgeId.Empty)
                    {
                        continue;
                    }

                    // check type and return if needed.
                    var returnEdge = bestEdgeSnapPoint.isOrthoganal || nonOrthogonalEdges;
                    if (!returnEdge)
                    {
                        continue;
                    }

                    // calculate the actual offset.
                    var offset = ushort.MaxValue;
                    if (bestEdgeSnapPoint.offset < length)
                    {
                        if (bestEdgeSnapPoint.offset <= 0)
                        {
                            offset = 0;
                        }
                        else
                        {
                            offset = (ushort)(bestEdgeSnapPoint.offset / length * ushort.MaxValue);
                        }
                    }

                    // invert offset if edge is reversed.
                    if (!edgeEnumerator.Forward)
                    {
                        offset = (ushort)(ushort.MaxValue - offset);
                    }

                    yield return new SnapPoint(bestEdgeSnapPoint.edgeId, offset);
                } // while (edgeEnumerator.MoveNext())
            } // for (uint v = 0; v < tile.VertexCount; v++)
        } // foreach (var (tileId, tile) in candidates)
    }

    /// <summary>
    /// Returns the closest vertex to the center of the given box.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="searchBox">The box to search in.</param>
    /// <param name="maxDistance">The maximum distance of any vertex returned relative to the center of the search box.</param>
    /// <param name="edgeChecker">Used to determine if an edge is acceptable or not. If null any edge will be accepted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The closest edge to the center of the box inside the given box.</returns>
    public static async Task<VertexId> SnapToVertexInBoxAsync(this RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) searchBox, IEdgeChecker? edgeChecker = null, double maxDistance = double.MaxValue, CancellationToken cancellationToken = default)
    {
        var center = searchBox.Center();
        var closestDistance = maxDistance;
        var closest = VertexId.Empty;

        var vertices = network.SearchVerticesInBox(searchBox);
        var edgeEnumerator = network.GetEdgeEnumerator();
        foreach (var (vertex, location) in vertices)
        {
            var d = center.DistanceEstimateInMeter(location);
            if (d > closestDistance) continue;

            if (edgeChecker == null)
            {
                closest = vertex;
                closestDistance = d;
                continue;
            }

            edgeEnumerator.MoveTo(vertex);
            while (edgeEnumerator.MoveNext())
            {
                if (!(edgeChecker.IsAcceptable(edgeEnumerator) ?? await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken))) continue;

                closest = vertex;
                closestDistance = d;
                break;
            }
        }

        return closest;
    }

    /// <summary>
    /// Snaps all vertices in the given box that could potentially be snapping vertices.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="searchBox">The box to search in.</param>
    /// <param name="maxDistance">The maximum distance of any vertex returned relative to the center of the search box.</param>
    /// <param name="edgeChecker">Used to determine if an edge is acceptable or not. If null any edge will be accepted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All the vertices within the box with at least one acceptable edge.</returns>
    public static async IAsyncEnumerable<VertexId> SnapToAllVerticesInBoxAsync(this RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) searchBox,
        IEdgeChecker? edgeChecker = null, double maxDistance = double.MaxValue, CancellationToken cancellationToken = default)
    {
        var center = searchBox.Center();
        var vertices = network.SearchVerticesInBox(searchBox);
        var edgeEnumerator = network.GetEdgeEnumerator();
        foreach (var (vertex, location) in vertices)
        {
            edgeEnumerator.MoveTo(vertex);

            var d = center.DistanceEstimateInMeter(location);
            if (d > maxDistance) continue;

            while (edgeEnumerator.MoveNext())
            {
                if (edgeChecker != null && !(edgeChecker.IsAcceptable(edgeEnumerator) ?? await edgeChecker.RunCheckAsync(edgeEnumerator, cancellationToken))) continue;

                yield return vertex;
                break;
            }
        }
    }

    /// <summary>
    /// Enumerates all edges that have at least one vertex in the given bounding box.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="box">The box to enumerate in.</param>
    /// <returns>An enumerator with all the vertices and their location.</returns>
    public static IEdgeEnumerator<RoutingNetwork> SearchEdgesInBox(this RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box)
    {
        var vertices = network.SearchVerticesInBox(box);
        return new VertexEdgeEnumerator(network, vertices.Select((i) => i.vertex));
    }

    /// <summary>
    /// Returns true if the edge's bounding box could possibly produce a snap
    /// point closer than <paramref name="bestDistance"/> meters from
    /// <paramref name="center"/>. Used as a cheap prefilter before the
    /// full per-segment projection loop. Returns false → caller can skip
    /// the edge entirely.
    ///
    /// The bbox is built inline by streaming tail + shape + head once and
    /// taking min/max on each axis. Intentionally not cached: a per-tile
    /// or per-edge cache costs memory in proportion to graph size, which
    /// matters for country/continent-scale router DBs. The per-snap cost
    /// is one extra shape iteration per candidate edge, all arithmetic and
    /// comparisons, no haversines and no allocations beyond the shape
    /// enumerator we'd allocate anyway.
    /// </summary>
    // Sentinel for "effectively unbounded" cutoffs. Earth's circumference is
    // ~4×10⁷ m, so any distance above this threshold can never prune a real
    // edge — and importantly catches the very common `double.MaxValue` cutoff
    // (which is finite by C# rules and so isn't caught by IsInfinity).
    private const double UnboundedDistanceCutoff = 1e10;

    private static bool EdgeBboxCanBeat(
        IEdgeEnumerator<RoutingNetwork> enumerator,
        (double longitude, double latitude, float? e) center,
        double bestDistance)
    {
        if (bestDistance >= UnboundedDistanceCutoff || double.IsNaN(bestDistance)) return true;

        var tail = enumerator.TailLocation;
        var head = enumerator.HeadLocation;
        var minLon = tail.longitude < head.longitude ? tail.longitude : head.longitude;
        var maxLon = tail.longitude > head.longitude ? tail.longitude : head.longitude;
        var minLat = tail.latitude < head.latitude ? tail.latitude : head.latitude;
        var maxLat = tail.latitude > head.latitude ? tail.latitude : head.latitude;

        foreach (var (sLon, sLat, _) in enumerator.Shape)
        {
            if (sLon < minLon) minLon = sLon;
            else if (sLon > maxLon) maxLon = sLon;
            if (sLat < minLat) minLat = sLat;
            else if (sLat > maxLat) maxLat = sLat;
        }

        // Closest point on the bbox to the query, clamped per axis.
        var cLon = center.longitude < minLon ? minLon
            : center.longitude > maxLon ? maxLon : center.longitude;
        var cLat = center.latitude < minLat ? minLat
            : center.latitude > maxLat ? maxLat : center.latitude;
        var closest = (cLon, cLat, (float?)null);
        return closest.DistanceEstimateInMeter(center) <= bestDistance;
    }

    /// <summary>
    /// Tile-level predicate from snap-algorithm.md. Returns false if no edge
    /// with a vertex in this tile can possibly produce a snap within
    /// <paramref name="bestDistance"/> of the query, given the tile's own
    /// max edge extents.
    /// </summary>
    private static bool TileCanReach(
        (double minLon, double minLat, double maxLon, double maxLat) tileBbox,
        double tileMaxLonDiff,
        double tileMaxLatDiff,
        (double longitude, double latitude, float? e) center,
        double bestDistance)
    {
        if (bestDistance >= UnboundedDistanceCutoff || double.IsNaN(bestDistance)) return true;

        var lonClosest = center.longitude < tileBbox.minLon ? tileBbox.minLon
            : center.longitude > tileBbox.maxLon ? tileBbox.maxLon : center.longitude;
        var latClosest = center.latitude < tileBbox.minLat ? tileBbox.minLat
            : center.latitude > tileBbox.maxLat ? tileBbox.maxLat : center.latitude;

        var dLon = Math.Abs(center.longitude - lonClosest) - tileMaxLonDiff;
        if (dLon < 0) dLon = 0;
        var dLat = Math.Abs(center.latitude - latClosest) - tileMaxLatDiff;
        if (dLat < 0) dLat = 0;
        if (dLon == 0 && dLat == 0) return true;

        // Convert the (dLon, dLat) offset at center.lat into meters by
        // measuring the haversine distance from center to a point offset by
        // exactly that much. Direction sign doesn't matter for distance.
        var probe = (center.longitude + dLon, center.latitude + dLat, (float?)null);
        return probe.DistanceEstimateInMeter(center) <= bestDistance;
    }

    /// <summary>
    /// Per-vertex predicate from snap-algorithm.md. Same shape as
    /// <see cref="TileCanReach"/> but the "closest point of the tile bbox"
    /// becomes the vertex itself (the actual point, not a clamp).
    /// </summary>
    private static bool VertexCanReach(
        double vLon, double vLat,
        double tileMaxLonDiff,
        double tileMaxLatDiff,
        (double longitude, double latitude, float? e) center,
        double bestDistance)
    {
        if (bestDistance >= UnboundedDistanceCutoff || double.IsNaN(bestDistance)) return true;

        var dLon = Math.Abs(center.longitude - vLon) - tileMaxLonDiff;
        if (dLon < 0) dLon = 0;
        var dLat = Math.Abs(center.latitude - vLat) - tileMaxLatDiff;
        if (dLat < 0) dLat = 0;
        if (dLon == 0 && dLat == 0) return true;

        var probe = (center.longitude + dLon, center.latitude + dLat, (float?)null);
        return probe.DistanceEstimateInMeter(center) <= bestDistance;
    }

    /// <summary>
    /// Yields the tile coordinates on the perimeter of the square at
    /// <paramref name="ring"/> tiles around (<paramref name="cx"/>, <paramref name="cy"/>).
    /// Ring 0 is just the centre tile. Tile coords are unsigned, so the
    /// caller must filter against valid (in-box) bounds. We compute via
    /// <c>long</c> internally to handle the unsigned underflow safely when
    /// the centre tile is near (0, 0).
    /// </summary>
    private static IEnumerable<(uint x, uint y)> TilesInRing(uint cx, uint cy, uint ring)
    {
        if (ring == 0)
        {
            yield return (cx, cy);
            yield break;
        }

        var startX = (long)cx - ring;
        var endX = (long)cx + ring;
        var startY = (long)cy - ring;
        var endY = (long)cy + ring;

        // Top row (y = startY), full width.
        if (startY >= 0)
        {
            for (var x = startX; x <= endX; x++)
                if (x >= 0) yield return ((uint)x, (uint)startY);
        }

        // Right column (x = endX), excluding the top-right corner already yielded.
        for (var y = startY + 1; y <= endY; y++)
            if (y >= 0) yield return ((uint)endX, (uint)y);

        // Bottom row (y = endY), right-to-left, excluding the bottom-right corner.
        for (var x = endX - 1; x >= startX; x--)
            if (x >= 0) yield return ((uint)x, (uint)endY);

        // Left column (x = startX), bottom-to-top, excluding both corners.
        if (startX >= 0)
        {
            for (var y = endY - 1; y >= startY + 1; y--)
                if (y >= 0) yield return ((uint)startX, (uint)y);
        }
    }

    /// <summary>
    /// Distance between two unsigned tile indices, returning 0 if the second
    /// would underflow the first. Used to size the outermost ring needed to
    /// cover the search box without doing signed arithmetic on uints.
    /// </summary>
    private static long SafeDelta(uint a, uint b) => a >= b ? (long)(a - b) : 0L;

    /// <summary>
    /// Used only as a sort key: distance from the query to the tile bbox's
    /// closest point, ignoring the tile's max edge extents. Smaller = closer
    /// tile, processed first → bestDistance shrinks faster → more aggressive
    /// pruning on subsequent tiles.
    /// </summary>
    private static double ClosestBoxDistanceMeters(
        (double minLon, double minLat, double maxLon, double maxLat) bbox,
        (double longitude, double latitude, float? e) center)
    {
        var lonClosest = center.longitude < bbox.minLon ? bbox.minLon
            : center.longitude > bbox.maxLon ? bbox.maxLon : center.longitude;
        var latClosest = center.latitude < bbox.minLat ? bbox.minLat
            : center.latitude > bbox.maxLat ? bbox.maxLat : center.latitude;
        var probe = (lonClosest, latClosest, (float?)null);
        return probe.DistanceEstimateInMeter(center);
    }
}
