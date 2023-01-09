using System.IO;
using Itinero.IO;
using Itinero.Network.Serialization;

namespace Itinero;

public sealed partial class RouterDb
{
    /// <summary>
    /// Reads a router db from the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The router db as read from the stream.</returns>
    public static RouterDb ReadFrom(Stream stream, RouterDbReadSettings? settings = null)
    {
        settings ??= new RouterDbReadSettings();

        return new(stream, settings);
    }

    /// <summary>
    /// Writes the router db to the given stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public void WriteTo(Stream stream)
    {
        // write version #.
        stream.WriteVarInt32(1);

        // write network.
        using (var mutable = this.GetMutableNetwork())
        {
            mutable.WriteTo(stream);
        }

        // write edge type map data.
        _edgeTypeIndex.WriteTo(stream);

        // write turn cost type map data.
        _turnCostTypeIndex.WriteTo(stream);

        // write attributes.
        this.WriteAttributesTo(stream);
    }
}
