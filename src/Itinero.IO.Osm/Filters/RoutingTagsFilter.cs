using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;

namespace Itinero.IO.Osm.Filters
{
    /// <summary>
    /// The default built-in routing tags filter.
    /// </summary>
    internal static class RoutingTagsFilter
    {
        /// <summary>
        /// Gets the default tags filter.
        /// </summary>
        public static readonly TagsFilter Default = new () {
            Filter = RoutingTagsFilter.Filter,
            MemberFilter = RoutingTagsFilter.ProcessCycleNetwork
        };
            
        private static bool Filter(OsmGeo osmGeo)
        {
            switch (osmGeo.Type) {
                case OsmGeoType.Way:
                    return FilterWay(osmGeo);
                case OsmGeoType.Node:
                    break;
                case OsmGeoType.Relation:
                    if (osmGeo.Tags == null || osmGeo.Tags.Count == 0) return false;
                    break;
                default:
                    break;
            }
            
            return true;
        }

        private static bool FilterWay(OsmGeo osmGeo)
        {
            if (osmGeo.Tags == null) return false;
            
            foreach (var t in osmGeo.Tags) {
                if (t.Key == "highway") {
                    return true;
                }
                else if (t.Key == "route" && t.Value == "ferry") {
                    return true;
                }
            }

            return false;
        }

        private const string CycleNetworkPrefix = "_cycle_network";
        
        private static bool ProcessCycleNetwork(Relation relation, OsmGeo? member)
        {
            if (relation.Members == null) return false;
            if (relation.Tags == null) return false;

            var network = string.Empty;
            var type = string.Empty;
            var route = string.Empty;
            foreach (var t in relation.Tags) {
                if (t.Key == "type" && t.Value == "route") {
                    type = "route";
                } else if (t.Key == "route" && t.Value == "bicycle") {
                    route = "bicycle";
                } else if (t.Key == "network") {
                    network = t.Value;
                }
            }
            if (string.IsNullOrWhiteSpace(type) || 
                string.IsNullOrWhiteSpace(route) || 
                string.IsNullOrWhiteSpace(network)) return false;

            if (member?.Tags != null && member.Type == OsmGeoType.Way) {
                member.Tags[$"{CycleNetworkPrefix}:network:{network}"] = "yes   ";
            }
            
            return true;
        }
    }
}