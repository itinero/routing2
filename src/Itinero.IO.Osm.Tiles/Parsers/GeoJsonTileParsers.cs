/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.LocalGeo;
using Itinero.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace Itinero.IO.Osm.Tiles.Parsers
{
    internal static class GeoJsonTileParser
    {
        // TODO: linked-data magic to automagically discover the correct URL and zoom-level.

        /// <summary>
        /// The base url to fetch the tiles from.
        /// </summary>
        private const string BaseUrl = "http://tiles.itinero.tech";

        private const string Vertex1AttributeName = "vertex1";
        private const string Vertex2AttributeName = "vertex2";

        private static readonly JsonSerializer GeoJsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
        
        /// <summary>
        /// Adds data from an individual tile.
        /// </summary>
        /// <param name="routerDb">The router db to fill.</param>
        /// <param name="globalIdMap">The global id map.</param>
        /// <param name="tile">The tile to load.</param>
        internal static void AddOsmTile(this RouterDb routerDb, GlobalIdMap globalIdMap, Tile tile)
        {
            var url = BaseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}.geojson";
            var stream = Download.DownloadHelper.Download(url);
            if (stream == null)
            {
                return;
            }

            FeatureCollection features = null;
            try
            {
                features =
                    GeoJsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(new StreamReader(stream)));
            }
            catch (Exception ex)
            {
                Logging.Logger.Log(nameof(GeoJsonTileParser), TraceEventType.Error, 
                    $"Cannot parse content of tile {tile}: {ex}");
                return;
            }

            Logger.Log(nameof(GeoJsonTileParser), Logging.TraceEventType.Information,
                $"Loading tile {tile}");
            
            var localIdMap = new Dictionary<long, VertexId>();
            foreach (var feature in features.Features)
            {
                if (!(feature.Geometry is LineString lineString)) continue;
                
                // parse attributes.
                var attributes = new AttributeCollection();
                var names = feature.Attributes.GetNames();
                var values = feature.Attributes.GetValues();
                var global1 = Constants.GLOBAL_ID_EMPTY;
                var global2 = Constants.GLOBAL_ID_EMPTY;
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    if (name == Vertex1AttributeName)
                    {
                        if (!values[i].TryParseGlobalId(out global1))
                        {
                            global1 = Constants.GLOBAL_ID_EMPTY;
                        }
                        continue;
                    }
                    if (name == Vertex2AttributeName)
                    {
                        if (!values[i].TryParseGlobalId(out global2))
                        {
                            global2 = Constants.GLOBAL_ID_EMPTY;
                        }
                        continue;
                    }
                    
                    // any other data considered valid attributes for now.
                    if (values[i].TryParseAttributeValue(out var value))
                    {
                        attributes.AddOrReplace(name, value);
                    }
                }
                if (global1 == Constants.GLOBAL_ID_EMPTY ||
                    global2 == Constants.GLOBAL_ID_EMPTY)
                {
                    if (attributes.ContainsKey("id") &&
                        attributes.ContainsKey("zoom"))
                    { // this is the tile outline.
                        continue;
                    }
                    // this is pretty severe, don't fail but log.
                    Logger.Log(nameof(GeoJsonSerializer), TraceEventType.Error,
                        $"Tile {tile} contains a linestring feature without valid vertex ids.");
                    continue;
                }
                
                // get the vertex locations and check if they are inside the tile or not.
                var vertex1Location = new Coordinate(lineString.Coordinates[0].X, lineString.Coordinates[0].Y);
                var vertex2Location = new Coordinate(lineString.Coordinates[lineString.Coordinates.Length - 1].X, 
                    (float)lineString.Coordinates[lineString.Coordinates.Length - 1].Y);
                var vertex1Outside = !tile.IsInside(vertex1Location.Longitude, vertex1Location.Latitude);
                var vertex2Outside = !tile.IsInside(vertex2Location.Longitude, vertex1Location.Longitude);

                // figure out if vertices are already mapped, if yes, get their ids, if not add them.
                var vertex1Global = false;
                if ((vertex1Outside || vertex2Outside) &&
                    globalIdMap.TryGet(global1, out var vertex1))
                {
                    vertex1Global = true;
                }
                else
                {
                    if (!localIdMap.TryGetValue(global1, out vertex1))
                    {
                        // no vertex yet, create one.
                        vertex1 = routerDb.AddVertex(vertex1Location.Longitude, vertex1Location.Latitude);
                        localIdMap[global1] = vertex1;
                    }
                }
                var vertex2Global = false;
                if ((vertex1Outside || vertex2Outside) &&
                    globalIdMap.TryGet(global2, out var vertex2))
                {
                    vertex2Global = true;
                }
                else
                {
                    if (!localIdMap.TryGetValue(global2, out vertex2))
                    {
                        // no vertex yet, create one.
                        vertex2 = routerDb.AddVertex(vertex2Location.Longitude, vertex2Location.Latitude);
                        localIdMap[global2] = vertex2;
                    }
                }
                
//                // add the edge if needed.
//                if (vertex1Global || vertex2Global)
//                { // edge was already added in another tile.
//                    continue;
//                }
//                
                // add vertices to the global map if one of them is outside.
                if (vertex1Outside || vertex2Outside)
                {
                    globalIdMap.Set(global1, vertex1);
                    globalIdMap.Set(global2, vertex2);
                }
                
                // calculate distance and build shape.
                var shape = new List<Coordinate>();
                for (var i = 0; i < lineString.Coordinates.Length; i++)
                {
                    var shapePoint = new Coordinate(lineString.Coordinates[i].X, lineString.Coordinates[i].Y);
                    shape.Add(shapePoint);
                }
                shape.RemoveAt(0);
                shape.RemoveAt(shape.Count - 1);

                routerDb.AddEdge(vertex1, vertex2, attributes, shape);
            }
        }

        internal static bool TryParseGlobalId(this object value, out long globalId)
        {
            if (value is int intValue)
            {
                globalId = intValue;
                return true;
            }
            else if (value is long longValue)
            {
                globalId = longValue;
                return true;
            }

            if (value != null &&
                long.TryParse(value.ToString(), out globalId))
            {
                return true;
            }

            globalId = 0;
            return false;
        }

        internal static bool TryParseAttributeValue(this object value, out string attribute)
        {
            if (value == null)
            {
                attribute = string.Empty;
                return true;
            }

            attribute = value.ToString();
            return true;
        }
    }
}