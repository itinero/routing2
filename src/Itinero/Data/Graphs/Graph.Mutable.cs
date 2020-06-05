using System;
using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;

namespace Itinero.Data.Graphs
{
    internal sealed partial class Graph
    {
        private MutableGraph? _mutableGraph = null;

        internal IMutableGraph GetAsMutable()
        {
            lock (_writeSync) // make sure no writer can be created while the mutable graph is being created.
            {
                if (_mutableGraph != null)
                    throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
                if (_writer != null)
                    throw new InvalidOperationException("Cannot get mutable version when there is a writer active.");

                _mutableGraph = new MutableGraph(this);
                return _mutableGraph;
            }
        }

        internal void CloneTileIfNeeded(GraphTile tile, int edgeTypesId)
        {
            var mutableGraph = _mutableGraph;
            if (mutableGraph != null && !mutableGraph.HasTile(tile.TileId)) mutableGraph.SetTile(tile.Clone(), edgeTypesId);
        }

        internal void ClearMutable()
        {
            _mutableGraph = null;
        }
        
        internal class MutableGraph : IMutableGraph
        {
            private readonly SparseArray<bool> _modified;
            private readonly SparseArray<(GraphTile tile, int edgeTypesId)> _tiles;
            private readonly Graph _graph;
            private GraphEdgeTypeIndex _graphEdgeTypeIndex;

            public MutableGraph(Graph graph)
            {
                _graph = graph;
                _tiles = graph._tiles.Clone();
                _graphEdgeTypeIndex = graph._graphEdgeTypeIndex;
                
                _modified = new SparseArray<bool>(_tiles.Length);
            }

            internal bool HasTile(uint localTileId)
            {
                return _tiles.Length > localTileId && 
                       _modified[localTileId] == true;
            }

            internal void SetTile(GraphTile tile, int edgeTypesId)
            {
                _tiles[tile.TileId] = (tile, edgeTypesId);
                _modified[tile.TileId] = true;
            }

            private GraphTile GetTileForWrite(uint localTileId)
            {
                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);
                
                // check if there is already a modified version.
                var (tile, edgeTypesId) = _tiles[localTileId];
                if (tile != null)
                {
                    if (edgeTypesId == _graphEdgeTypeIndex.Id) return tile;
                    
                    tile = _graphEdgeTypeIndex.Update(tile);
                    _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                    return tile;
                }
                
                // there is no tile, get the one from the graph or create a new one.
                tile = new GraphTile(_graph.Zoom, localTileId);
                
                // store in the local tiles.
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                return tile;
            }

            private GraphTile? GetTileForRead(uint localTileId)
            {
                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);
                
                // check if there is already a modified version.
                var (tile, edgeTypesId) = _tiles[localTileId];
                if (tile == null) return null;
                
                // update the tile if needed.
                if (edgeTypesId == _graphEdgeTypeIndex.Id) return tile;
                tile = _graphEdgeTypeIndex.Update(tile);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                return tile;
            }

            public VertexId AddVertex(double longitude, double latitude)
            {
                // get the local tile id.
                var (x, y) = TileStatic.WorldToTile(longitude, latitude, _graph.Zoom);
                var localTileId = TileStatic.ToLocalId(x, y, _graph.Zoom);

                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);

                // get the tile (or create it).
                var tile = this.GetTileForWrite(localTileId);
                
                // add the vertex.
                return tile.AddVertex(longitude, latitude);
            }

            public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
            {
                var localTileId = vertex.TileId;

                // get tile.
                if (_tiles.Length <= localTileId)
                {
                    longitude = default;
                    latitude = default;
                    return false;
                }

                var tile = this.GetTileForRead(localTileId);
                if (tile == null)
                {
                    longitude = default;
                    latitude = default;
                    return false;
                }

                // check if the vertex exists.
                return tile.TryGetVertex(vertex, out longitude, out latitude);
            }

            public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
                IEnumerable<(double longitude, double latitude)>? shape = null,
                IEnumerable<(string key, string value)>? attributes = null)
            {
                var tile = this.GetTileForWrite(vertex1.TileId);
                if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

                var edgeTypeId = attributes != null ? (uint?)_graphEdgeTypeIndex.Get(attributes) : null;
                var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId);
                if (vertex1.TileId != vertex2.TileId)
                {
                    // this edge crosses tiles, also add an extra edge to the other tile.
                    tile = this.GetTileForWrite(vertex2.TileId);
                    tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId);
                }

                return edge1;
            }

            public GraphEdgeTypeFunc EdgeTypeFunc => _graphEdgeTypeIndex.Func;

            public void SetEdgeTypeFunc(GraphEdgeTypeFunc graphEdgeTypeFunc)
            {
                _graphEdgeTypeIndex = _graphEdgeTypeIndex.Next(graphEdgeTypeFunc);
            }

            public Graph ToGraph()
            {
                return new Graph(_tiles, _graph.Zoom, _graphEdgeTypeIndex);
            }

            public void Dispose()
            {
                _graph.ClearMutable();
            }
        }
    }
}