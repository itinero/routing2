using System;
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
    public static void AddStandaloneTile(this RoutingNetworkWriter writer, StandaloneNetworkTile tile, IGlobalIdSet globalIdSet)
    {
        // add the tile without boundary crossings.
        writer.AddTile(tile.NetworkTile);
        
        // add the boundary crossings for tiles that are already loaded.
        foreach (var crossing in tile.GetBoundaryCrossings()) {
            // add crossings in target vertex already in global id set.
            var other = crossing.isToTile ? crossing.globalIdFrom : crossing.globalIdTo;
            if (globalIdSet.TryGet(other, out var otherVertexId)) {
                if (crossing.isToTile) {
                    writer.AddEdge(otherVertexId, crossing.vertex,
                        ArraySegment<(double longitude, double latitude, float? e)>.Empty,
                        crossing.attributes, crossing.edgeTypeId, crossing.length);
                } else {
                    writer.AddEdge(crossing.vertex, otherVertexId,
                        ArraySegment<(double longitude, double latitude, float? e)>.Empty,
                        crossing.attributes, crossing.edgeTypeId, crossing.length);
                }
            }
            
            // update global id set with the vertex in the tile.
            globalIdSet.Set(crossing.isToTile ? crossing.globalIdTo : crossing.globalIdFrom, crossing.vertex);
        }
    }
}