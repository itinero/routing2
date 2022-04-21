using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network.Tiles;
// ReSharper disable PossibleMultipleEnumeration

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
        private readonly IRoutingNetworkWritable _network;

        internal RoutingNetworkWriter(IRoutingNetworkWritable network)
        {
            _network = network;
        }

        public VertexId AddVertex(double longitude, double latitude, float? elevation = null)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

            // get the tile (or create it).
            var (tile, _) = _network.GetTileForWrite(localTileId);

            return tile.AddVertex(longitude, latitude, elevation);
        }

        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null, uint? edgeTypeId = null,
            uint? length = null)
        {
            // get the tile (or create it).
            var (tile, edgeTypeMap) = _network.GetTileForWrite(vertex1.TileId);
            if (tile == null) {
                throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            }

            // get the edge type id.
            edgeTypeId ??= attributes != null ? edgeTypeMap(attributes) : null;

            // get the edge length in centimeters.
            if (!_network.TryGetVertex(vertex1, out var longitude, out var latitude, out var e)) {
                throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex1} not found.");
            }

            var vertex1Location = (longitude, latitude, e);
            if (!_network.TryGetVertex(vertex2, out longitude, out latitude, out e)) {
                throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex2} not found.");
            }

            var vertex2Location = (longitude, latitude, e);

            length ??= (uint) (vertex1Location.DistanceEstimateInMeterShape(
                vertex2Location, shape) * 100);

            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId, length);
            if (vertex1.TileId == vertex2.TileId) {
                return edge1;
            }

            // this edge crosses tiles, also add an extra edge to the other tile.
            (tile, _) = _network.GetTileForWrite(vertex2.TileId);
            tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId, length);

            return edge1;
        }

        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            if (prefix != null) {
                throw new NotSupportedException($"Turn costs with {nameof(prefix)} not supported.");
            }

            // get the tile (or create it).
            var (tile, _) = _network.GetTileForWrite(vertex.TileId);
            if (tile == null) {
                throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
            }

            // get the turn cost type id.
            var turnCostMap = _network.RouterDb.GetTurnCostTypeMap();
            var turnCostTypeId = turnCostMap.func(attributes);

            // add the turn cost table using the type id.
            tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs);
        }

        internal void AddTile(NetworkTile tile)
        {
            _network.SetTile(tile);
        }

        internal bool HasTile(uint localTileId)
        {
            return _network.HasTile(localTileId);
        }

        public void Dispose()
        {
            _network.ClearWriter();
        }
    }
}