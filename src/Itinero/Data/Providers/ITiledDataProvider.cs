using System.Collections.Generic;
using Itinero.Network.Tiles.Standalone;

namespace Itinero.Data.Providers;

/// <summary>
/// A provider that serves prebuilt tiles, for something else to install.
/// </summary>
/// <remarks>
/// Separate from <see cref="DataProviderBase"/> because a provider that writes the network itself
/// — the PBF import — builds no standalone tiles and should not be made to. Implemented alongside
/// the base, not instead of it.
/// </remarks>
public interface ITiledDataProvider
{
    /// <summary>
    /// Gets the tile at the given coordinates.
    /// </summary>
    StandaloneNetworkTile GetTile(uint x, uint y);

    /// <summary>
    /// Enumerates the tiles this provider has.
    /// </summary>
    /// <remarks>
    /// Without this a provider can only ever be driven by demand, because something else would
    /// have to already know which tiles to ask for. With it, "load everything" is a loop.
    /// </remarks>
    IEnumerable<(uint x, uint y)> GetTiles();
}
