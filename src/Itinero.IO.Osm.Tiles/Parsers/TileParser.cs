using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data.Attributes;
using Itinero.Data.Shapes;
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
        /// Adds data from an individual tile.
        /// </summary>
        /// <param name="routerDb">The router db to fill.</param>
        /// <param name="globalIdMap">The global id map.</param>
        /// <param name="tile">The tile to load.</param>
        /// <param name="baseUrl">The base url of the routeable tile source.</param>
        internal static void AddOsmTile(this RouterDb routerDb, GlobalIdMap globalIdMap, Tile tile, string baseUrl = BaseUrl)
        {
            var url = baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
            var stream = Download.DownloadHelper.Download(url);
            if (stream == null)
            {
                return;
            }

            Logger.Log(nameof(TileParser), Logging.TraceEventType.Information,
                $"Loading tile: {tile}");
            
            var nodeLocations = new Dictionary<long, Coordinate>();
            using (var textReader = new StreamReader(stream))
            {
                var jsonObject = JObject.Parse(textReader.ReadToEnd());

                if (!(jsonObject["@graph"] is JArray graph)) return;

                foreach (var graphObject in graph)
                {
                    if (!(graphObject["@id"] is JToken idToken)) continue;
                    var id = idToken.Value<string>();

                    if (id == null) continue;

                    if (id.StartsWith("http://www.openstreetmap.org/node/"))
                    {
                        var nodeId = long.Parse(id.Substring("http://www.openstreetmap.org/node/".Length,
                            id.Length - "http://www.openstreetmap.org/node/".Length));

                        if (globalIdMap.TryGet(nodeId, out var vertexId)) continue;

                        if (!(graphObject["geo:long"] is JToken longToken)) continue;
                        var lon = longToken.Value<double>();
                        if (!(graphObject["geo:lat"] is JToken latToken)) continue;
                        var lat = latToken.Value<double>();
                        
                        nodeLocations[nodeId] = new Coordinate((float) lat, (float) lon);
                    }
                    else if (id.StartsWith("http://www.openstreetmap.org/way/"))
                    {
                        var attributes = new AttributeCollection();
                        foreach (var child in graphObject.Children())
                        {
                            if (!(child is JProperty property)) continue;

                            if (property.Name == "@id" ||
                                property.Name == "osm:nodes" ||
                                property.Name == "@type") continue;

                            if (property.Name == "rdfs:label")
                            {
                                attributes.AddOrReplace("name", property.Value.Value<string>());
                                continue;
                            }

                            var key = property.Name;
                            if (key.StartsWith("osm:"))
                            {
                                key = key.Substring(4, key.Length - 4);
                            }

                            var value = property.Value.Value<string>();
                            if (value.StartsWith("osm:"))
                            {
                                value = value.Substring(4, value.Length - 4);
                            }

                            attributes.AddOrReplace(key, value);
                        }

                        if (!(graphObject["osm:nodes"] is JArray nodes)) continue;
                        
                        // add first as vertex.
                        var node = nodes[0];
                        if (!(node is JToken nodeToken)) continue;
                        var nodeIdString = nodeToken.Value<string>();
                        var nodeId = long.Parse(nodeIdString.Substring("http://www.openstreetmap.org/node/".Length,
                            nodeIdString.Length - "http://www.openstreetmap.org/node/".Length));
                        if (!globalIdMap.TryGet(nodeId, out var previousVertex))
                        {
                            if (!nodeLocations.TryGetValue(nodeId, out var nodeLocation))
                            {
                                throw new Exception($"Could not load tile {tile}: node {nodeId} missing.");
                            }
                            previousVertex = routerDb.AddVertex(nodeLocation.Longitude, nodeLocation.Latitude);
                            globalIdMap.Set(nodeId, previousVertex);
                        }
                        
                        // add last as vertex.
                        node = nodes[nodes.Count - 1];
                        nodeToken = (node as JToken);
                        if (nodeToken == null) continue;
                        nodeIdString = nodeToken.Value<string>();
                        nodeId = long.Parse(nodeIdString.Substring("http://www.openstreetmap.org/node/".Length,
                            nodeIdString.Length - "http://www.openstreetmap.org/node/".Length));
                        if (!globalIdMap.TryGet(nodeId, out var vertexId))
                        {
                            if (!nodeLocations.TryGetValue(nodeId, out var nodeLocation))
                            {
                                throw new Exception($"Could not load tile {tile}: node {nodeId} missing.");
                            }
                            vertexId = routerDb.AddVertex(nodeLocation.Longitude, nodeLocation.Latitude);
                            globalIdMap.Set(nodeId, vertexId);
                        }
                        
                        var shape = new List<Coordinate>();
                        for (var n = 1; n < nodes.Count; n++)
                        {
                            node = nodes[n];
                            nodeToken = node as JToken;
                            if (node == null) continue;
                            nodeIdString = nodeToken.Value<string>();
                            nodeId = long.Parse(nodeIdString.Substring("http://www.openstreetmap.org/node/".Length,
                                nodeIdString.Length - "http://www.openstreetmap.org/node/".Length));

                            if (globalIdMap.TryGet(nodeId, out vertexId))
                            {
                                routerDb.AddEdge(previousVertex, vertexId, attributes, new ShapeEnumerable(shape));
                                shape.Clear();

                                previousVertex = vertexId;
                            }
                            else
                            {
                                if (!nodeLocations.TryGetValue(nodeId, out var nodeLocation))
                                {
                                    throw new Exception($"Could not load tile {tile}: node {nodeId} missing.");
                                }
                                shape.Add(nodeLocation);
                            }
                        }
                    }
                    else if (id.StartsWith("http://www.openstreetmap.org/relation/"))
                    {
                        Console.WriteLine(id);
                    }
                }
            }
        }
    }
}