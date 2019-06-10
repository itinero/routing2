using System.Collections.Generic;
using Itinero.Data.Attributes;
using Newtonsoft.Json;
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
        public static Attribute? Map(this JProperty property, Dictionary<string, TagMapperConfig> reverseMappings)
        {
            if (!reverseMappings.TryGetValue(property.Name, out var mapperConfig)) return null;

            if (property.Value == null)
            {
                return new Attribute(mapperConfig.OsmKey, null);
            }

            var valueString = property.Value.ToInvariantString();
            if (mapperConfig.ReverseMapping == null)
            {
                return new Attribute(mapperConfig.OsmKey, valueString);
            }
            if (mapperConfig.ReverseMapping.TryGetValue(valueString, out var reverseMapped))
            {
                return new Attribute(mapperConfig.OsmKey, reverseMapped);
            }

            return null;
        }
    }
}