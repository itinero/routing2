using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Itinero.IO.Osm.Tiles.Parsers.Semantics
{
    internal static class TagMapper
    {
        /// <summary>
        /// Reverse maps the given property using the given semantic mappings.
        /// </summary>
        /// <param name="property">The property to map.</param>
        /// <param name="reverseMappings">The reverse mappings.</param>
        /// <returns>True if there was a mapping for this tag.</returns>
        public static (string key, string value)? Map(this JProperty property,
            Dictionary<string, TagMapperConfig> reverseMappings)
        {
            if (!reverseMappings.TryGetValue(property.Name, out var mapperConfig))
            {
                return null;
            }

            if (property.Value == null)
            {
                return (mapperConfig.OsmKey, null);
            }

            var valueString = property.Value.ToInvariantString();
            if (mapperConfig.ReverseMapping == null)
            {
                return (mapperConfig.OsmKey, valueString);
            }

            if (mapperConfig.ReverseMapping.TryGetValue(valueString, out var reverseMapped))
            {
                return (mapperConfig.OsmKey, reverseMapped);
            }

            return null;
        }
    }
}
