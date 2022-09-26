using System.Collections;
using System.Collections.Generic;
using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Vertices
{
    internal class TilesVertexEnumerator : IEnumerator<VertexId>
    {
        private readonly RoutingNetwork _routingNetwork;
        private readonly IEnumerator<(uint x, uint y)> _tiles;

        public TilesVertexEnumerator(RoutingNetwork routingNetwork, IEnumerable<(uint x, uint y)> tiles)
        {
            _routingNetwork = routingNetwork;
            _tiles = tiles.GetEnumerator();
        }

        public void Reset()
        {
            _tiles.Reset();
            _currentTile = uint.MaxValue;
        }

        object IEnumerator.Current => Current;

        private uint _currentTile = uint.MaxValue;
        private uint _currentVertex = uint.MaxValue;
        private double _currentLatitude;
        private double _currentLongitude;
        private float? _currentElevation;

        public bool MoveNext()
        {
            if (_currentTile == uint.MaxValue)
            {
                while (_tiles.MoveNext())
                {
                    _currentTile = TileStatic.ToLocalId(_tiles.Current.x, _tiles.Current.y, _routingNetwork.Zoom);
                    _currentVertex = 0;

                    if (_routingNetwork.TryGetVertex(new VertexId(_currentTile, _currentVertex),
                            out _currentLongitude, out _currentLatitude, out _currentElevation))
                    {
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                _currentVertex++;
                if (_routingNetwork.TryGetVertex(new VertexId(_currentTile, _currentVertex),
                        out _currentLongitude, out _currentLatitude, out _currentElevation))
                {
                    return true;
                }
                else
                {
                    if (!_tiles.MoveNext())
                    {
                        break;
                    }

                    _currentTile = TileStatic.ToLocalId(_tiles.Current.x, _tiles.Current.y, _routingNetwork.Zoom);
                    _currentVertex = 0;
                    if (_routingNetwork.TryGetVertex(new VertexId(_currentTile, _currentVertex),
                            out _currentLongitude, out _currentLatitude, out _currentElevation))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public VertexId Current => new(_currentTile, _currentVertex);

        public (double longitude, double latitude, float? e) Location =>
            (_currentLongitude, _currentLatitude, _currentElevation);

        public void Dispose()
        {
            _tiles.Dispose();
        }
    }
}