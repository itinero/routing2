using System.Collections.Generic;
using System.Linq;
using OsmSharp;

namespace Itinero.IO.Osm.Filters {
    /// <summary>
    /// The default built-in routing tags filter.
    /// </summary>
    public sealed class RoutingTagsFilter : ITagsFilter {
        private RoutingTagsFilter() { }

        /// <inheritdoc/>
        public IEnumerable<(string key, string value)>? Filter(OsmGeo osmGeo) {
            if (osmGeo.Tags == null) {
                return null;
            }

            switch (osmGeo.Type) {
                case OsmGeoType.Node:
                case OsmGeoType.Relation:
                    return osmGeo.Tags.Select(x => (x.Key, x.Value));
                case OsmGeoType.Way:
                    return FilterWay(osmGeo);
            }

            return null;
        }

        private static IEnumerable<(string key, string value)>? FilterWay(OsmGeo osmGeo) {
            foreach (var t in osmGeo.Tags) {
                if (t.Key == "highway") {
                    return osmGeo.Tags.Select(x => (x.Key, x.Value));
                }
                else if (t.Key == "route" && t.Value == "ferry") {
                    return osmGeo.Tags.Select(x => (x.Key, x.Value));
                }
            }

            return null;
        }

        /// <summary>
        /// The default routing tags filter instance.
        /// </summary>
        public static RoutingTagsFilter Default = new();
    }
}