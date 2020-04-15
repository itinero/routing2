using System;
using System.Globalization;
using System.IO;

namespace Itinero.IO.Osm.Tiles
{
    internal static class Extensions
    {
        /// <summary>
        /// Returns a string representing the object in a culture invariant way.
        /// </summary>
        internal static string ToInvariantString(this object obj)
        {
            return obj is IConvertible convertible ? convertible.ToString(CultureInfo.InvariantCulture)
                : obj is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : obj.ToString();
        }
        
        /// <summary>
        /// Loads a string from an embedded resource stream.
        /// </summary>
        internal static Stream LoadEmbeddedResourceStream(string name)
        {
            return typeof(GlobalIdMap).Assembly.GetManifestResourceStream(name);
        }
    }
}