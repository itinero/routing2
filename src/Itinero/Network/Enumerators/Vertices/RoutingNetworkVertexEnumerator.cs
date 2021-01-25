using System.Collections.Generic;
using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Vertices
{ 
    internal class RoutingNetworkVertexEnumerator
    {
        private readonly RoutingNetwork _routingNetwork;
        private readonly IEnumerator<uint> _tileEnumerator;

        public RoutingNetworkVertexEnumerator(RoutingNetwork routingNetwork)
        {
            _routingNetwork = routingNetwork;
            _tileEnumerator = routingNetwork.GetTileEnumerator();

            this.Current = VertexId.Empty;
        }

        private long _localId = -1;
        private uint _tileId = uint.MaxValue;
        private NetworkTile? _tile;

        private void MoveNexTile()
        {
            while (true)
            {
                if (!_tileEnumerator.MoveNext()) return;

                // get tile for reading.
                _tileId = _tileEnumerator.Current;
                _tile = _routingNetwork.GetTileForRead(_tileId);
                
                if (_tile != null) return;
            }
        }

        public bool MoveNext()
        {
            while (true)
            {
                // when vertex is empty move to the first tile.
                if (this.Current.IsEmpty())
                {
                    this.MoveNexTile();
                }

                // no current tile here, no data.
                if (_tile == null) return false;

                // move to the next vertex.
                _localId++;
                this.Current = new VertexId(_tileId, (uint) _localId);

                // get vertex, if it exists, return true.
                // TODO: this check can be done faster without reading coordinates.
                if (_tile.TryGetVertex(this.Current, out _, out _, out _)) return true;

                // vertex doesn't exist, move to the next tile.
                _localId = -1;
                this.MoveNexTile();
            }
        }

        public VertexId Current { get; private set; }

        public void Reset()
        {
            this.Current = VertexId.Empty;
            _localId = -1;
            _tileId = uint.MaxValue;

            _tileEnumerator.Reset();
        }
    }
}