using Itinero.Attributes;
using Itinero.IO.Json;
using Itinero.LocalGeo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Itinero.Tiled
{
    public static class RouterDbTiledExtensions
    {
        /// <summary>
        /// Gets all features inside the given bounding box and builds a geojson string.
        /// </summary>
        public static string GetGeoJson(this RouterDbTiled db, uint tileId, bool includeEdges = true,
            bool includeVertices = true, bool includeBounds = false)
        {
            var stringWriter = new StringWriter();
            db.WriteGeoJson(stringWriter, tileId, includeEdges, includeVertices, includeBounds);
            return stringWriter.ToInvariantString();
        }

        public static void WriteGeoJson(this RouterDbTiled db, TextWriter writer, uint tileId, bool includeEdges = true,
            bool includeVertices = true, bool includeBounds = false)
        {
            if (db == null) { throw new ArgumentNullException("db"); }
            if (writer == null) { throw new ArgumentNullException("writer"); }

            var jsonWriter = new JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            var edges = new HashSet<long>();

            var tile = db.Graph.GetTile(tileId);
            var tileObject = Tiles.Tile.FromLocalId(tile.Zoom, tileId);

            if (includeBounds)
            {
                tileObject.WriteTile(jsonWriter);
            }

            var edgeEnumerator = tile.GetEdgeEnumerator(0);
            for (uint vertex = 0; vertex < tile.VertexCount; vertex++)
            {
                if (includeVertices)
                {
                    tile.WriteVertex(jsonWriter, vertex);
                }

                if (includeEdges)
                {
                    edgeEnumerator.MoveTo(vertex);
                    edgeEnumerator.Reset();

                    while (edgeEnumerator.MoveNext())
                    {
                        if (edges.Contains(edgeEnumerator.Id))
                        {
                            continue;
                        }
                        if (edgeEnumerator.DataInverted)
                        {
                            continue;
                        }
                        edges.Add(edgeEnumerator.Id);
                        
                        db.WriteEdge(jsonWriter, tile, edgeEnumerator);
                    }
                }
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();
        }
        
        /// <summary>
        /// Writes a point-geometry for the given vertex.
        /// </summary>
        internal static void WriteVertex(this VertexTile db, JsonWriter jsonWriter, uint vertex)
        {
            var coordinate = db.GetVertex(vertex);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Point", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();
            jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
            jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("id", vertex.ToInvariantString());
            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }

        public static List<Coordinate> GetShape(this RouterDbTiled db, VertexTile.EdgeEnumerator edgeEnumerator)
        {
            var result = new List<Coordinate>();
            result.Add(edgeEnumerator.VertexTile.GetVertex(edgeEnumerator.From));

            var shape = edgeEnumerator.Shape;
            if (shape != null)
            {
                if (edgeEnumerator.DataInverted)
                {
                    shape = shape.Reverse();
                }

                result.AddRange(shape);
            }

            result.Add(db.Graph.GetVertex(edgeEnumerator.GlobalTo));
            return result;
        }

        internal static void WriteTile(this Tiles.Tile tile, JsonWriter jsonWriter)
        {
            var corners = new Coordinate[]
            {
                Tiles.Tile.TileIndexToWorld(tile.X, tile.Y, tile.Zoom),
                Tiles.Tile.TileIndexToWorld(tile.X + 1, tile.Y, tile.Zoom),
                Tiles.Tile.TileIndexToWorld(tile.X + 1, tile.Y + 1, tile.Zoom),
                Tiles.Tile.TileIndexToWorld(tile.X, tile.Y + 1, tile.Zoom),
                Tiles.Tile.TileIndexToWorld(tile.X, tile.Y, tile.Zoom)
            };

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();

            foreach (var coordinate in corners)
            {
                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("id", tile.LocalId.ToInvariantString(), true, true);
            jsonWriter.WriteProperty("zoom", tile.Zoom.ToInvariantString(), true, true);
            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }
        
        /// <summary>
        /// Writes a linestring-geometry for the edge currently in the enumerator.
        /// </summary>
        internal static void WriteEdge(this RouterDbTiled db, JsonWriter jsonWriter, VertexTile tile, VertexTile.EdgeEnumerator edgeEnumerator)
        {

            var edgeAttributes = new Itinero.Attributes.AttributeCollection(tile.EdgeMeta.Get(edgeEnumerator.Data[1]));
            float distance;
            ushort profile;
            Itinero.Data.Edges.EdgeDataSerializer.Deserialize(edgeEnumerator.Data[0], out distance, out profile);
            edgeAttributes.AddOrReplace(db.EdgeProfiles.Get(profile));

            var shape = db.GetShape(edgeEnumerator);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "Feature", true, false);
            jsonWriter.WritePropertyName("geometry", false);

            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "LineString", true, false);
            jsonWriter.WritePropertyName("coordinates", false);
            jsonWriter.WriteArrayOpen();

            foreach (var coordinate in shape)
            {
                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(coordinate.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(coordinate.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();
            }

            jsonWriter.WriteArrayClose();
            jsonWriter.WriteClose();

            jsonWriter.WritePropertyName("properties");
            jsonWriter.WriteOpen();
            if (edgeAttributes != null)
            {
                foreach (var attribute in edgeAttributes)
                {
                    jsonWriter.WriteProperty(attribute.Key, attribute.Value, true, true);
                }
            }
            jsonWriter.WriteProperty("edgeid", edgeEnumerator.Id.ToInvariantString());
            jsonWriter.WriteProperty("vertex1", edgeEnumerator.From.ToInvariantString());
            jsonWriter.WriteProperty("vertex2", edgeEnumerator.To.ToInvariantString());
            jsonWriter.WriteClose();

            jsonWriter.WriteClose();
        }

    }
}
