using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.Data.Shapes;
using Itinero.IO.Osm.Tiles.Parsers.Semantics;
using Itinero.LocalGeo;
using Itinero.Logging;
using Newtonsoft.Json.Linq;

namespace Itinero.IO.Osm.Tiles.Parsers
{
    public static class TileParser
    {
        /// <summary>
        /// The base url to fetch the tiles from.
        /// </summary>
        public const string BaseUrl = "https://tiles.openplanner.team/planet";

        /// <summary>
        /// The function to download from a given url.
        /// </summary>
        public static Func<string, Stream> DownloadFunc = Download.DownloadHelper.Download;
        
        private static readonly Lazy<Dictionary<string, TagMapperConfig>> ReverseMappingLazy = new Lazy<Dictionary<string, TagMapperConfig>>(
            () => TagMapperConfigParser.Parse(Extensions.LoadEmbeddedResourceStream("Itinero.IO.Osm.Tiles.ontology.mapping_config.json")));

        /// <summary>
        /// Adds data from an individual tile.
        /// </summary>
        /// <param name="routerDb">The router db to fill.</param>
        /// <param name="globalIdMap">The global id map.</param>
        /// <param name="tile">The tile to load.</param>
        /// <param name="baseUrl">The base url of the routeable tile source.</param>
        internal static bool AddOsmTile(this RouterDb routerDb, GlobalIdMap globalIdMap, Tile tile, string baseUrl = BaseUrl)
        {
            var url = baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
            var stream = DownloadFunc(url);
            if (stream == null)
            {
                return false;
            }

            Logger.Log(nameof(TileParser), Logging.TraceEventType.Information,
                $"Loading tile: {tile}");
            
            var nodeLocations = new Dictionary<long, Coordinate>();
            var waysData = new Dictionary<long, (List<long> nodes, AttributeCollection attributes)>();
            var nodes = new HashSet<long>();
            var coreNodes = new HashSet<long>();
            var updated = false;
            using (var textReader = new StreamReader(stream))
            {
                var json = textReader.ReadToEnd();
                var jsonObject = JObject.Parse(json);

                if (!(jsonObject["@graph"] is JArray graph)) return false;

                foreach (var graphObject in graph)
                {
                    if (!(graphObject["@id"] is JToken idToken)) continue;
                    var id = idToken.Value<string>();

                    if (id == null) continue;

                    if (id.StartsWith("http://www.openstreetmap.org/node/"))
                    { // parse as a node.
                        var nodeId = long.Parse(id.Substring("http://www.openstreetmap.org/node/".Length,
                            id.Length - "http://www.openstreetmap.org/node/".Length));

                        if (globalIdMap.TryGet(nodeId, out var _))
                        { // this node was marked core in another tile, make sure it's core here too. 
                            coreNodes.Add(nodeId);
                            continue;
                        }

                        if (!(graphObject["geo:long"] is JToken longToken)) continue;
                        var lon = longToken.Value<double>();
                        if (!(graphObject["geo:lat"] is JToken latToken)) continue;
                        var lat = latToken.Value<double>();
                        
                        nodeLocations[nodeId] = new Coordinate((float) lon, (float) lat);
                    }
                    else if (id.StartsWith("http://www.openstreetmap.org/way/"))
                    { // parse as a way.
                        var wayId = long.Parse(id.Substring("http://www.openstreetmap.org/way/".Length,
                            id.Length - "http://www.openstreetmap.org/way/".Length));
                        
                        // interpret all tags with defined semantics.
                        var attributes = GetTags(graphObject ,ReverseMappingLazy.Value);
                        attributes.AddOrReplace("way_id", wayId.ToInvariantString());
                        attributes.AddOrReplace("tile_x", tile.X.ToInvariantString());
                        attributes.AddOrReplace("tile_y", tile.Y.ToInvariantString());
                        
                        // include all raw tags (if any).
                        if ((graphObject["osm:hasTag"] is JArray rawTags))
                        {
                            for (var n = 0; n < rawTags.Count; n++)
                            {
                                var rawTag = rawTags[n];
                                if (!(rawTag is JValue rawTagValue)) continue;

                                var keyValue = rawTagValue.Value<string>();
                                var keyValueSplit = keyValue.Split('=');
                                if (keyValueSplit.Length != 2) continue;
                                
                                attributes.AddOrReplace(keyValueSplit[0], keyValueSplit[1]);
                            }
                        }

                        // parse nodes.
                        if (!(graphObject["osm:hasNodes"] is JArray wayNodes)) continue;

                        var nodeIds = new List<long>();
                        for (var n = 0; n < wayNodes.Count; n++)
                        {
                            var nodeToken = wayNodes[n];
                            var nodeIdString = nodeToken.Value<string>();
                            var nodeId = long.Parse(nodeIdString.Substring("http://www.openstreetmap.org/node/".Length,
                                nodeIdString.Length - "http://www.openstreetmap.org/node/".Length));
                            nodeIds.Add(nodeId);
                                
                            if (n == 0 || n == wayNodes.Count - 1)
                            { // first and last nodes always core.
                                coreNodes.Add(nodeId);
                            }
                            else if (nodes.Contains(nodeId))
                            { // second time this node was hit.
                                coreNodes.Add(nodeId);
                            }
                            nodes.Add(nodeId);
                        }

                        waysData[wayId] = (nodeIds, attributes);
                    }
                    else if (id.StartsWith("http://www.openstreetmap.org/relation/"))
                    { // parse as a relation.
                        // TODO: parse as a relation.
                    }
                }

                foreach (var wayPairs in waysData)
                {
                    var wayNodes = wayPairs.Value.nodes;
                    var attributes = wayPairs.Value.attributes;

                    var shape = new List<Coordinate>();
                    var previousVertex = VertexId.Empty;
                    for (var n = 0; n < wayNodes.Count; n++)
                    {
                        var nodeId = wayNodes[n];
                        
                        if (coreNodes.Contains(nodeId))
                        {      
                            if (!globalIdMap.TryGet(nodeId, out var vertex))
                            {
                                if (!nodeLocations.TryGetValue(nodeId, out var vertexLocation))
                                {
                                    Itinero.Logging.Logger.Log(nameof(TileParser), TraceEventType.Warning,
                                        $"Could not load way {wayPairs.Key} in {tile}: node {nodeId} missing.");
                                    break;
                                }
                                
                                vertex = routerDb.AddVertex(vertexLocation.Longitude, vertexLocation.Latitude);
                                globalIdMap.Set(nodeId, vertex);
                                updated = true;
                            }

                            if (!previousVertex.IsEmpty())
                            {
                                routerDb.AddEdge(previousVertex, vertex, attributes, new ShapeEnumerable(shape));
                                updated = true;
                                shape.Clear();
                            }

                            previousVertex = vertex;
                            continue;
                        }
                        
                        if (!nodeLocations.TryGetValue(nodeId, out var nodeLocation))
                        {
                            Itinero.Logging.Logger.Log(nameof(TileParser), TraceEventType.Warning,
                                $"Could not load way {wayPairs.Key} in {tile}: node {nodeId} missing.");
                            break;
                        }

                        shape.Add(nodeLocation);
                    }
                }

                return updated;
            }
        }

        /// <summary>
        /// Gets the OSM tags from the given node/way or relation.
        /// </summary>
        /// <param name="osmGeo">The node, way or relation json-ld part.</param>
        /// <param name="reverseMappings">The reverse mappings.</param>
        /// <returns>The tags.</returns>
        private static AttributeCollection GetTags(JToken osmGeo, Dictionary<string, TagMapperConfig> reverseMappings)
        {
            var attributes = new AttributeCollection();
                        
            // interpret all tags with defined semantics.
            foreach (var child in osmGeo.Children())
            {
                if (!(child is JProperty property)) continue;

                if (property.Name == "@id" ||
                    property.Name == "@type") continue;
                if (property.Value is JArray) continue;

                var attribute = property.Map(reverseMappings);
                if (attribute == null) continue;

                attributes.AddOrReplace(attribute.Value.Key, attribute.Value.Value);
            }

            return attributes;
        }
    }
}