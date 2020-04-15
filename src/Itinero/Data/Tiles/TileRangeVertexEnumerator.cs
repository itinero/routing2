using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero.Data.Tiles
{
    /// <summary>
    /// An enumerator that enumerates all vertices in a tile range.
    /// </summary>
    public class TileRangeVertexEnumerator : IEnumerator<VertexId>
    {
        private readonly Graph _graph;
        private readonly TileRange _tileRange;
        private readonly IEnumerator<Tile> _tileEnumerator;

        public TileRangeVertexEnumerator(TileRange tileRange, Graph graph)
        {
            if (tileRange.Zoom != graph.Zoom) throw new ArgumentException("Cannot enumerate vertices based on a tile range when it's zoom level doesn't match the graph zoom level.");
            
            _tileRange = tileRange;
            _tileEnumerator = _tileRange.GetEnumerator();
            _graph = graph;
        }

        public void Reset()
        {
            _tileEnumerator.Reset();
        }

        object IEnumerator.Current => Current;

        private uint _currentTile = uint.MaxValue;
        private uint _currentVertex = uint.MaxValue;
        private double _currentLatitude;
        private double _currentLongitude;
        
        public bool MoveNext()
        {
            if (_currentTile == uint.MaxValue)
            {
                while (_tileEnumerator.MoveNext())
                {
                    _currentTile = _tileEnumerator.Current.LocalId;
                    _currentVertex = 0;

                    if (_graph.TryGetVertex(new VertexId(_currentTile, _currentVertex), 
                        out _currentLongitude, out _currentLatitude))
                    {
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                _currentVertex++;
                if (_graph.TryGetVertex(new VertexId(_currentTile, _currentVertex), 
                    out _currentLongitude, out _currentLatitude))
                {
                    return true;
                }
                else
                {
                    if (!_tileEnumerator.MoveNext())
                    { 
                        break;
                    }
                    
                    _currentTile = _tileEnumerator.Current.LocalId;
                    _currentVertex = 0;
                }
            }

            return false;
        }



        public VertexId Current => new VertexId(_currentTile, _currentVertex);

        public (double longitude, double latitude) Location => (_currentLongitude, _currentLatitude);

        public void Dispose()
        {
            
        }
    }
}