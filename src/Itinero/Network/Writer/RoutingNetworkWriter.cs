using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network.Tiles;

namespace Itinero.Network.Writer
{
    /// <summary>
    /// A writer to write to an instance. This writer will never change existing data, only add new data.
    ///
    /// This writer can:
    /// - add new vertices and edges.
    ///
    /// This writer cannot mutate existing data, only add new.
    /// </summary>
    public class RoutingNetworkWriter : IDisposable
    {
        private readonly IRoutingNetworkWritable _graph;

        internal RoutingNetworkWriter(IRoutingNetworkWritable graph)
        {
            _graph = graph;
        }
        
        public VertexId AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, _graph.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, _graph.Zoom);

            // get the tile (or create it).
            var tile = _graph.GetTileForWrite(localTileId);

            return tile.AddVertex(longitude, latitude);
        }
        
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            // get the tile (or create it).
            var tile = _graph.GetTileForWrite(vertex1.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            
            // get the edge type id.
            var edgeTypeId = attributes != null ? (uint?)_graph.GraphTurnCostTypeIndex.Get(attributes) : null;
            
            // get the edge length in centimeters.
            if (!_graph.TryGetVertex(vertex1, out var longitude, out var latitude))
            {
                throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex1} not found.");
            }
            var vertex1Location = (longitude, latitude);
            if (!_graph.TryGetVertex(vertex2, out longitude, out latitude))
            {
                throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex2} not found.");
            }
            var vertex2Location = (longitude, latitude);
            
            var length = (uint)(vertex1Location.DistanceEstimateInMeterShape(
                vertex2Location, shape) * 100);
            
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId, length);
            if (vertex1.TileId == vertex2.TileId) return edge1;
            
            // this edge crosses tiles, also add an extra edge to the other tile.
            tile = _graph.GetTileForWrite(vertex2.TileId);
            tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId, length);

            return edge1;
        }

        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            if (prefix != null) throw new NotSupportedException($"Turn costs with {nameof(prefix)} not supported.");
            
            // get the tile (or create it).
            var tile = _graph.GetTileForWrite(vertex.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
            
            // get the turn cost type id.
            var turnCostTypeId = _graph.GraphTurnCostTypeIndex.Get(attributes);
                
            // add the turn cost table using the type id.
            tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs);
        }

        public void Dispose()
        {
            _graph.ClearWriter();
        }
    }
}