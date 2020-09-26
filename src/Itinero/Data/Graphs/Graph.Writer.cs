using System;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.Writing;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs
{
    public sealed partial class Graph : IGraphWritable
    {
        private readonly object _writeSync = new object();
        private GraphWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        internal bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        internal GraphWriter GetWriter()
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

        GraphTurnCostTypeIndex IGraphWritable.GraphTurnCostTypeIndex => _graphTurnCostTypeIndex;
        
        GraphTile IGraphWritable.GetTileForWrite(uint localTileId)
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
                    this.CloneTileIfNeededForMutator(tile, edgeTypesId);
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

        private void CloneTileIfNeededForMutator(GraphTile tile, int edgeTypesId)
        {
            // this is weird right?
            //
            // the combination these features make this needed:
            // - we don't want to clone every tile when we read data in the mutable graph so we use the exiting tiles.
            // - a graph can be written to at all times (to lazy load data) but can be mutated at any time too.
            // 
            // this makes it possible the graph is being written to and mutated at the same time.
            // we need to check, when writing to a graph, a mutator doesn't have the tile in use or
            // date from the write could bleed into the mutator creating an invalid state.
            // so **we have to clone tiles before writing to them and give them to the mutator**
            var mutableGraph = _graphMutator;
            if (mutableGraph != null && !mutableGraph.HasTile(tile.TileId)) mutableGraph.SetTile(tile.Clone(), edgeTypesId);
        }
    }
}