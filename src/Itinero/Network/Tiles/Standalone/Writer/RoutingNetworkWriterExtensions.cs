using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data;
using Itinero.Network.Writer;

namespace Itinero.Network.Tiles.Standalone.Writer;

/// <summary>
/// Extension methods related to writing standalone tiles to a network.
/// </summary>
public static class RoutingNetworkWriterExtensions
{
    /// <summary>
    /// Adds a tile in the form of a standalone tile to the network.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="tile">The tile to add.</param>
    /// <param name="globalIdSet">The global id set.</param>
    public static void AddStandaloneTile(this RoutingNetworkWriter writer, StandaloneNetworkTile tile,
        GlobalNetworkManager globalIdSet)
    {
        // add the tile without boundary crossings.
        writer.AddTile(tile.NetworkTile);

        // add the boundary crossings for tiles that are already loaded.
        var boundaryEdges = new Dictionary<BoundaryEdgeId, EdgeId>();
        foreach (var crossing in tile.GetBoundaryCrossings())
        {
            // add crossings in target vertex already in global id set.
            var other = crossing.isToTile ? crossing.globalIdFrom : crossing.globalIdTo;
            if (globalIdSet.VertexIdSet.TryGet(other, out var otherVertexId))
            {
                // add the edge.
                EdgeId newEdge;
                if (crossing.isToTile)
                {
                    newEdge = writer.AddEdge(otherVertexId, crossing.vertex,
                        ArraySegment<(double longitude, double latitude, float? e)>.Empty,
                        crossing.attributes, crossing.edgeTypeId, crossing.length);
                }
                else
                {
                    newEdge = writer.AddEdge(crossing.vertex, otherVertexId,
                        ArraySegment<(double longitude, double latitude, float? e)>.Empty,
                        crossing.attributes, crossing.edgeTypeId, crossing.length);
                }

                // register globally and index crossings.
                boundaryEdges[crossing.id] = newEdge;
            }

            // update global id set with the vertex in the tile.
            globalIdSet.VertexIdSet.Set(crossing.isToTile ? crossing.globalIdTo : crossing.globalIdFrom,
                crossing.vertex);
        }

        // read and store all global edge ids.
        foreach (var (globalEdgeId, edgeId, boundaryEdgeId) in tile.GetGlobalEdgeIds())
        {
            if (edgeId != null)
            {
                globalIdSet.EdgeIdSet.Set(globalEdgeId, edgeId.Value);
                continue;
            }

            if (boundaryEdgeId == null) throw new Exception("global edge has to have at least one edge id");
            if (!boundaryEdges.TryGetValue(boundaryEdgeId.Value, out var newEdgeId)) continue;

            globalIdSet.EdgeIdSet.Set(globalEdgeId, newEdgeId);
        }

        // add boundary crossing turn cost or register globally.
        foreach (var crossingTurnCosts in tile.GetGlobalTurnCost())
        {
            // check if all edges are in the network and fetch them.
            var hasAllEdges = true;
            var edges = crossingTurnCosts.edges.Select(x =>
            {
                if (globalIdSet.EdgeIdSet.TryGet(x.globalEdgeId, out var newEdgeId)) return (newEdgeId, x.forward);

                hasAllEdges = false;
                return (EdgeId.Empty, false);
            }).ToArray();

            // if all edges are there add turn costs.
            // if not all edges are there it will be added when the last tile containing an edge for this restriction is added.
            if (hasAllEdges)
            {
                // figure out what vertex the turn costs need to be added at.
                var edgeEnumerator = writer.GetEdgeEnumerator();
                if (!edgeEnumerator.MoveToEdge(edges[^1].Item1, edges[^1].Item2))
                    throw new Exception("edge should exist");
                var turnCostVertex = edgeEnumerator.Tail;

                writer.AddTurnCosts(turnCostVertex, crossingTurnCosts.attributes,
                    edges.Select(x => x.Item1).ToArray(),
                    crossingTurnCosts.costs, null, crossingTurnCosts.turnCostType);
            }
        }
    }
}
