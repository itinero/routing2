namespace Itinero.Network.Tiles.Standalone.Writer;

/// <summary>
/// Extensions related to standalone network tile writing.
/// </summary>
public static class RoutingNetworkExtensions
{
    /// <summary>
    /// Gets a writer to write to a brand new standalone tile.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="x">The x-coordinate of the tile.</param>
    /// <param name="y">The y-coordinate of the tile.</param>
    /// <returns></returns>
    public static StandaloneNetworkTileWriter GetStandaloneTileWriter(this RoutingNetwork network,
        uint x, uint y)
    {
        var zoom = network.Zoom;
        var localTileId = TileStatic.ToLocalId(x, y, zoom);
        var edgeTypeMap = network.RouterDb.GetEdgeTypeMap();
        var turnCostTypeMap = network.RouterDb.GetTurnCostTypeMap();
        
        var tile = new NetworkTile(zoom, localTileId, edgeTypeMap.id);
        var standaloneTile = new StandaloneNetworkTile(tile);

        return new StandaloneNetworkTileWriter(standaloneTile, zoom, edgeTypeMap, turnCostTypeMap);
    }
}