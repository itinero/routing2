namespace Itinero.Network.Tiles.Standalone;

/// <summary>
/// A standalone network tile.
/// </summary>
public partial class StandaloneNetworkTile
{
    internal StandaloneNetworkTile(NetworkTile networkTile)
    {
        _attributes = new byte[1024];
        _strings = new string[128];

        this.NetworkTile = networkTile;
    }

    internal NetworkTile NetworkTile { get; }

    /// <summary>
    /// Gets an enumerator to access the non-boundary edges.
    /// </summary>
    /// <returns></returns>
    public IStandaloneNetworkTileEnumerator GetEnumerator()
    {
        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(this.NetworkTile);
        return enumerator;
    }

    /// <summary>
    /// Gets the tile id.
    /// </summary>
    public uint TileId => this.NetworkTile.TileId;
}
