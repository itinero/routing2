using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Geo;
using Itinero.IO.Osm.Restrictions;
using Itinero.IO.Osm.Restrictions.Barriers;
using Itinero.Network;
using Itinero.Network.Tiles.Standalone;
using Itinero.Network.Tiles.Standalone.Writer;
using OsmSharp;

namespace Itinero.IO.Osm.Tiles;

/// <summary>
/// Contains extensions methods for the standalone tile writer.
/// </summary>
public static class StandaloneNetworkTileWriterExtensions
{
    /// <summary>
    /// Adds tile data using the writer and the given tile data.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="tileData">The OSM data in the tile.</param>
    /// <exception cref="Exception"></exception>
    public static void AddTileData(this StandaloneNetworkTileWriter writer, IEnumerable<OsmGeo> tileData)
    {
        // setup the edge type map.
        var edgeTypeMap = writer.EdgeTypeMap.func;
        var emptyEdgeType = edgeTypeMap(ArraySegment<(string key, string value)>.Empty);

        // keep all node locations.
        var nodeLocations = new Dictionary<long, (double longitude, double latitude, bool inside)>();
        // keep a set of used nodes, all nodes inside the tile.
        var usedNodes = new HashSet<long>();
        // keep a set of core nodes, all nodes inside the tile that should be a vertex
        var coreNodes = new HashSet<long>();
        // keep a set of boundary nodes, all boundary nodes (first node outside the tile).
        var boundaryNodes = new HashSet<long>();

        // first pass to:
        // - mark nodes are core, they are to be become vertices later.
        // - parse restrictions and keep restricted edges and mark nodes as core.
        using var enumerator = tileData.GetEnumerator();

        var osmTurnRestrictions = new List<OsmTurnRestriction>();
        var restrictedEdges = new Dictionary<Guid, BoundaryOrLocalEdgeId?>();
        var restrictionMembers = new Dictionary<long, Way?>();
        var restrictionParser = new OsmTurnRestrictionParser();

        var osmBarriers = new List<OsmBarrier>();
        var barrierParser = new OsmBarrierParser();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            switch (current)
            {
                case Node node:
                    if (node.Id == null) throw new Exception("Id cannot be null");
                    if (node.Longitude == null) throw new Exception("Longitude cannot be null");
                    if (node.Latitude == null) throw new Exception("Latitude cannot be null");

                    var nodeInTile = writer.IsInTile(node.Longitude.Value, node.Latitude.Value);
                    nodeLocations[node.Id.Value] = (node.Longitude.Value, node.Latitude.Value,
                        nodeInTile);

                    if (barrierParser.IsBarrier(node))
                    {
                        // make sure the barriers are core nodes, they need a turn cost.
                        // log nodes are barriers to be able to detect their ways,
                        //      only take nodes in the tile to mark as barrier.
                        coreNodes.Add(node.Id.Value);
                    }

                    break;
                case Way way when way.Nodes.Length == 0:
                    break;
                case Way way:
                    {
                        // calculate edge type and determine if there is relevant data.
                        var attributes = way.Tags?.Select(tag => (tag.Key, tag.Value)).ToArray() ??
                                         ArraySegment<(string key, string value)>.Empty;
                        var edgeTypeId = edgeTypeMap(attributes);
                        if (edgeTypeId == emptyEdgeType) continue;

                        // mark as core nodes used twice or nodes representing a boundary crossing.
                        bool? previousInTile = null;
                        for (var n = 0; n < way.Nodes.Length; n++)
                        {
                            var wayNode = way.Nodes[n];

                            // check if the node is in the tile or not.
                            if (!nodeLocations.TryGetValue(wayNode, out var value))
                            {
                                throw new Exception(
                                    $"Node {wayNode} as part of way {way.Id} not found in source data.");
                            }
                            var (_, _, inTile) = value;

                            // mark as core if used before and mark as used.
                            if (inTile)
                            {
                                // only mark nodes used when in the tile.
                                if (usedNodes.Contains(wayNode)) coreNodes.Add(wayNode);
                                usedNodes.Add(wayNode);

                                // if first or last node and inside always core.
                                if (n == 0 || n == way.Nodes.Length - 1)
                                {
                                    coreNodes.Add(wayNode);
                                }
                            }

                            // boundary crossing, from outside to in.
                            if (previousInTile is false && inTile == true)
                            {
                                boundaryNodes.Add(way.Nodes[n - 1]);
                                coreNodes.Add(way.Nodes[n]);
                            }

                            // boundary crossing, from inside to out.
                            if (previousInTile is true && inTile == false)
                            {
                                coreNodes.Add(way.Nodes[n - 1]);
                                boundaryNodes.Add(way.Nodes[n]);
                            }

                            previousInTile = inTile;
                        }

                        break;
                    }
                case Relation relation:
                    if (!restrictionParser.IsRestriction(relation, out _)) continue;

                    // log ways that are members, we need to keep their edge ids ready
                    // or store their global ids when the restriction crosses tile boundaries.
                    foreach (var relationMember in relation.Members)
                    {
                        if (relationMember.Type != OsmGeoType.Way) continue;

                        restrictionMembers[relationMember.Id] = null;
                    }

                    break;
            }
        }

        // a second pass where we add all vertices, core nodes and edges.
        // we also keep an index of edges that were added and are part of a restriction.
        var vertices = new Dictionary<long, VertexId>();
        enumerator.Reset();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            switch (current)
            {
                case Node node:
                    if (node.Id == null) throw new Exception("Id cannot be null");
                    if (node.Longitude == null) throw new Exception("Longitude cannot be null");
                    if (node.Latitude == null) throw new Exception("Latitude cannot be null");

                    // add all core nodes as vertices.
                    if (coreNodes.Contains(node.Id.Value))
                    {
                        if (!writer.IsInTile(node.Longitude.Value, node.Latitude.Value)) continue;
                        vertices[node.Id.Value] = writer.AddVertex(node.Longitude.Value, node.Latitude.Value);

                        // a core not can be a barrier, check here.
                        if (barrierParser.TryParse(node, out var barrier))
                        {
                            osmBarriers.Add(barrier);
                        }
                    }

                    break;
                case Way way when way.Nodes.Length == 0:
                    continue;
                case Way way:
                    {
                        if (restrictionMembers.ContainsKey(way.Id.Value)) restrictionMembers[way.Id.Value] = way;

                        var attributes = way.Tags?.Select(tag => (tag.Key, tag.Value)).ToArray() ??
                                         ArraySegment<(string key, string value)>.Empty;
                        var edgeTypeId = edgeTypeMap(attributes);
                        if (edgeTypeId == emptyEdgeType) continue;

                        // add all boundaries, if any.
                        for (var n = 1; n < way.Nodes.Length; n++)
                        {
                            var from = way.Nodes[n - 1];
                            var to = way.Nodes[n];
                            var globalEdgeId = way.GenerateGlobalEdgeId(n - 1, n);

                            var fromLocation = nodeLocations[from];
                            var toLocation = nodeLocations[to];

                            var length = (uint)((fromLocation.longitude, fromLocation.longitude, (float?)null)
                                .DistanceEstimateInMeter(
                                    (toLocation.longitude, toLocation.longitude, (float?)null)) * 100);

                            if (boundaryNodes.Contains(from) &&
                                vertices.TryGetValue(to, out var vertexId))
                            {
                                var boundaryEdgeId = writer.AddBoundaryCrossing(from, (vertexId, to),
                                    edgeTypeId, attributes.Concat(globalEdgeId), length);
                                if (restrictionMembers.ContainsKey(way.Id.Value))
                                    restrictedEdges[globalEdgeId] = new BoundaryOrLocalEdgeId(boundaryEdgeId);
                            }
                            else if (boundaryNodes.Contains(to) &&
                                     vertices.TryGetValue(from, out vertexId))
                            {
                                var boundaryEdgeId = writer.AddBoundaryCrossing((vertexId, from), to,
                                    edgeTypeId, attributes.Concat(globalEdgeId), length);
                                if (restrictionMembers.ContainsKey(way.Id.Value))
                                    restrictedEdges[globalEdgeId] = new BoundaryOrLocalEdgeId(boundaryEdgeId);
                            }
                        }

                        // add regular edges, if any.
                        var shape = new List<(double longitude, double latitude, float? e)>();
                        VertexId? previousVertex = null;
                        var previousNode = -1;

                        for (var n = 0; n < way.Nodes.Length; n++)
                        {
                            var wayNode = way.Nodes[n];

                            if (boundaryNodes.Contains(wayNode))
                            {
                                previousVertex = null;
                                shape.Clear();
                                continue;
                            }

                            if (!vertices.TryGetValue(wayNode, out var vertexId))
                            {
                                var (longitude, latitude, _) = nodeLocations[wayNode];

                                shape.Add((longitude, latitude, null));
                                continue;
                            }

                            if (previousVertex != null)
                            {
                                var globalEdgeId = way.GenerateGlobalEdgeId(previousNode, n);
                                var edgeId = writer.AddEdge(previousVertex.Value, vertexId, edgeTypeId, shape,
                                    attributes.Concat(globalEdgeId));
                                shape.Clear();

                                if (restrictionMembers.ContainsKey(way.Id.Value))
                                    restrictedEdges[globalEdgeId] = new BoundaryOrLocalEdgeId(edgeId);
                            }

                            previousVertex = vertexId;
                            previousNode = n;
                        }

                        break;
                    }
                case Relation relation:
                    var result = restrictionParser.TryParse(relation, (wayId) =>
                            restrictionMembers.TryGetValue(wayId, out var member) ? member : null,
                        out var osmTurnRestriction);
                    if (result.IsError) continue;
                    if (!result.Value) continue;
                    if (osmTurnRestriction == null)
                        throw new Exception("Parsing restriction was successful but not returned");

                    osmTurnRestrictions.Add(osmTurnRestriction);

                    break;
            }
        }

        // add barriers as turn weights.
        var tileEnumerator = writer.GetEnumerator();
        var networkRestrictions = new List<NetworkRestriction>();
        foreach (var osmBarrier in osmBarriers)
        {
            var networkBarriersResult = osmBarrier.ToNetworkRestrictions(n =>
            {
                if (!vertices.TryGetValue(n, out var v)) throw new Exception("Node should exist as vertex");
                tileEnumerator.MoveTo(v);
                return tileEnumerator;
            });
            if (networkBarriersResult.IsError) continue;

            networkRestrictions.AddRange(networkBarriersResult.Value);
        }

        // add restrictions as turn weights.
        foreach (var osmTurnRestriction in osmTurnRestrictions)
        {
            var networkRestrictionsResult = osmTurnRestriction.ToNetworkRestrictions((wayId, node1, node2) =>
            {
                var (globalId, forward) = GlobalEdgeIdExtensions.GenerateGlobalEdgeIdAndDirection(wayId, node1, node2);

                if (!restrictedEdges.TryGetValue(globalId, out var boundaryOrLocalEdgeId)) return null;

                if (boundaryOrLocalEdgeId?.LocalId != null) return (boundaryOrLocalEdgeId.Value.LocalId.Value, forward);

                return null;
            });
            if (networkRestrictionsResult.IsError) continue;

            networkRestrictions.AddRange(networkRestrictionsResult.Value);
        }

        // convert network restrictions to turn costs.
        foreach (var networkRestriction in networkRestrictions)
        {
            if (networkRestriction.Count < 2)
            {
                // TODO: log something?
                continue;
            }

            // get last edge and turn cost vertex.
            var last = networkRestriction[^1];
            var lastEdge = writer.GetEdge(last.edge, last.forward);
            var turnCostVertex = lastEdge.Tail;

            // only add turn costs around vertices that are in the current tile.
            if (turnCostVertex.TileId != writer.TileId) continue;

            var secondToLast = networkRestriction[^2];
            if (networkRestriction.IsProhibitory)
            {
                // easy, we only add a single cost.
                var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
                writer.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                    new[] { secondToLast.edge, last.edge }, costs,
                    networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
            }
            else
            {
                // hard, we need to add a cost for every *other* edge than then one in the restriction.
                tileEnumerator.MoveTo(secondToLast.edge, secondToLast.forward);
                var to = tileEnumerator.Head;
                tileEnumerator.MoveTo(to);

                while (tileEnumerator.MoveNext())
                {
                    if (tileEnumerator.EdgeId == secondToLast.edge ||
                        tileEnumerator.EdgeId == lastEdge.EdgeId) continue;

                    // easy, we only add a single cost.
                    var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
                    writer.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                        new[] { secondToLast.edge, tileEnumerator.EdgeId }, costs,
                        networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
                }
            }
        }

        // add global ids.
        foreach (var (globalEdgeId, restrictedEdge) in restrictedEdges)
        {
            if (restrictedEdge?.LocalId != null)
            {
                writer.AddGlobalIdFor(restrictedEdge.Value.LocalId.Value, globalEdgeId);
            }
            else if (restrictedEdge?.BoundaryId != null)
            {
                writer.AddGlobalIdFor(restrictedEdge.Value.BoundaryId.Value, globalEdgeId);
            }
        }

        // we can only add turn restrictions when all their edges 
        // are full within a single tile, when they are not we add them
        // to the tile as boundary restrictions using global edge ids.
    }

    internal static IStandaloneNetworkTileEnumerator GetEnumerator(this StandaloneNetworkTileWriter writer)
    {
        return writer.GetResultingTile().GetEnumerator();
    }
}
