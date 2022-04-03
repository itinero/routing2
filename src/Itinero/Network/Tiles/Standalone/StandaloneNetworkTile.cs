using Reminiscence.Arrays;

namespace Itinero.Network.Tiles.Standalone;

/// <summary>
/// A standalone network tile.
/// </summary>
public partial class StandaloneNetworkTile
{
    private readonly NetworkTile _networkTile;
    
    internal StandaloneNetworkTile(NetworkTile networkTile)
    {
        _attributes = new MemoryArray<byte>(1024);
        _strings = new MemoryArray<string>(128);
        
        _networkTile = networkTile;
    }

    internal NetworkTile NetworkTile => _networkTile;
    
    /// <summary>
    /// Gets the tile id.
    /// </summary>
    public uint TileId => _networkTile.TileId;
}