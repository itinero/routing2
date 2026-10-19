using System.IO;
using Itinero.IO;
using Itinero.Data.Providers;
using Itinero.Network.Serialization;

namespace Itinero;

public sealed partial class RouterDb
{
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

        // write attributes.
        this.WriteAttributesTo(stream);
    }
}
