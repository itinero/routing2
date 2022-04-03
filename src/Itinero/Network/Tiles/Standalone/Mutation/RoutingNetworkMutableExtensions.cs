using System;
using System.Collections.Generic;
using Itinero.Network.Mutation;

namespace Itinero.Network.Tiles.Standalone.Mutation;

public static class RoutingNetworkMutableExtensions
{
    /// <summary>
    /// Adds or updates the given tiles.
    /// </summary>
    /// <param name="mutator">The network mutator.</param>
    /// <param name="tiles">The tiles.</param>
    public static void AddOrUpdateTiles(this RoutingNetworkMutator mutator, IEnumerable<StandaloneNetworkTile> tiles)
    {
        // remove all data for tiles that exist.
        foreach (var tile in tiles) {
            var networkTile = mutator.GetTile(tile.TileId);
            if (networkTile == null) continue;

            throw new NotImplementedException();
            //networkTile.Clear();
        }
        
        // add the tiles again.
        foreach (var tile in tiles) {
            var networkTile = mutator.GetTile(tile.TileId);
            
        }
    }
}