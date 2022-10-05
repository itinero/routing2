using System;

namespace Itinero.Network.Tiles;

/// <summary>
/// Contains network tile extensions.
/// </summary>
internal static class NetworkTileExtensions
{
    /// <summary>
    /// Gets the location of the given vertex.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <param name="vertex">The vertex.</param>
    /// <returns>The location.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the vertex doesn't exist.</exception>
    public static (double longitude, double latitude, float? e) GetVertex(this NetworkTile tile,
        VertexId vertex)
    {
        if (!tile.TryGetVertex(vertex, out var longitude, out var latitude, out var e))
        {
            throw new ArgumentOutOfRangeException(nameof(vertex));
        }

        return (longitude, latitude, e);
    }
}
