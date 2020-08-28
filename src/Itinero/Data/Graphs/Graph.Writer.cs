using System;
using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;
using Itinero.Geo;

namespace Itinero.Data.Graphs
{
    internal sealed partial class Graph : IGraphWritable
    {
        private readonly object _writeSync = new object();
        
        private GraphTile GetTileForWrite(uint localTileId)
        {
            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);
            
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile != null)
            {
                if (edgeTypesId != _graphEdgeTypeIndex.Id)
                {
                    tile = _graphEdgeTypeIndex.Update(tile);
                    _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                }
                else
                {
                    // check if there is a mutable graph.
                    this.CloneTileIfNeeded(tile, edgeTypesId);
                }
            }
            
            if (tile == null)
            {
                // create a new tile.
                tile = new GraphTile(this.Zoom, localTileId);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
            }

            return tile;
        }

        private VertexId AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, this.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, this.Zoom);

            // get the tile (or create it).
            var tile = this.GetTileForWrite(localTileId);

            return tile.AddVertex(longitude, latitude);
        }
        
        private EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            // get the tile (or create it).
            var tile = this.GetTileForWrite(vertex1.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            
            // get the edge type id.
            var edgeTypeId = attributes != null ? (uint?)_graphEdgeTypeIndex.Get(attributes) : null;
            
            // get the edge length in centimeters.
            var length = (uint)(this.GetVertex(vertex1).DistanceEstimateInMeterShape(
                this.GetVertex(vertex2), shape) * 100);
            
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId, length);
            if (vertex1.TileId == vertex2.TileId) return edge1;
            
            // this edge crosses tiles, also add an extra edge to the other tile.
            tile = this.GetTileForWrite(vertex2.TileId);
            tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId, length);

            return edge1;
        }

        private void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs)
        {
            // get the tile (or create it).
            var tile = this.GetTileForWrite(vertex.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
            
            // get the turn cost type id.
            var turnCostTypeId = _graphTurnCostTypeIndex.Get(attributes);
                
            // add the turn cost table using the type id.
            tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs);
        }

        private GraphWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public GraphWriter GetWriter()
        {
            lock (_writeSync)
            {
                if (_writer != null)
                    throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                        $"Check {nameof(HasWriter)} to check for a current writer.");
                _writer = new GraphWriter(this);
                return _writer;
            }
        }
        
        void IGraphWritable.ClearWriter()
        {
            _writer = null;
        }

        /// <summary>
        /// A writer to write to an instance. This writer will never change existing data, only add new data.
        ///
        /// This writer can:
        /// - add new vertices and edges.
        ///
        /// This writer cannot mutate existing data, only add new.
        /// </summary>
        internal class GraphWriter : IDisposable
        {
            private readonly Graph _graph;

            public GraphWriter(Graph graph)
            {
                _graph = graph;
            }

            internal VertexId AddVertex(double longitude, double latitude)
            {
                return _graph.AddVertex(longitude, latitude);
            }

            internal EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
                IEnumerable<(double longitude, double latitude)>? shape = null,
                IEnumerable<(string key, string value)>? attributes = null)
            {
                return _graph.AddEdge(vertex1, vertex2, shape, attributes);
            }

            internal void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
                EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
            {
                if (prefix != null) throw new NotSupportedException($"Turn costs with {nameof(prefix)} not supported.");
                
                _graph.AddTurnCosts(vertex, attributes, edges, costs);
            }

            public void Dispose()
            {
                (_graph as IGraphWritable).ClearWriter();
            }
        }
    }
    
    internal interface IGraphWritable
    {
        void ClearWriter();
    }
}