using Itinero.IO.Osm.Tiles.Parsers;

namespace Itinero.IO.Osm.Tiles;

/// <summary>
/// The data provider settings.
/// </summary>
public class DataProviderSettings
{
    /// <summary>
    /// The url of the routeable tiles server.
    /// </summary>
    public string Url { get; set; } = TileParser.BaseUrl;

    /// <summary>
    /// The zoom level to consume tiles at.
    /// </summary>
    public uint Zoom { get; set; } = 14;
}
