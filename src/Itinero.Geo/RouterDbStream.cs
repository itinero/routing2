using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.Geo
{
    /// <summary>
    ///     Converts a routerDB into a stream of geofeatures
    /// </summary>
    public class RouterDbStream : IEnumerable<IFeature>
    {
        private readonly RoutingNetwork _network;

        public RouterDbStream(RoutingNetwork network)
        {
            _network = network;
        }

        public IEnumerator<IFeature> GetEnumerator()
        {
            return new RouterDbEnumerator(_network);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class RouterDbEnumerator : IEnumerator<IFeature>
    {
        private readonly RoutingNetwork _network;

        private readonly IEnumerator<VertexId> _vertexIds;


        private bool _verticesAreDepleted;
        private RoutingNetworkEdgeEnumerator _edges;

        public RouterDbEnumerator(RoutingNetwork network)
        {
            _network = network;
            _vertexIds = network.GetVertices().GetEnumerator();
           _edges = network.GetEdgeEnumerator();
        }

        public bool MoveNext()
        {
            _verticesAreDepleted = !_vertexIds.MoveNext();
            if (!_verticesAreDepleted) {
                var attrs = new AttributesTable{
                    {"_abs", "" + _vertexIds.Current}
                };
                _network.TryGetVertex(_vertexIds.Current, out var lon, out var lat, out var el);
                // 
                this.Current = new Feature(
                    new Point(lon, lat, el ?? 0f), attrs);

                return true;
            }

            var hasNextEdge = _edges.MoveNext();
            if (!hasNextEdge) {
                return false;
            }

            var attr = new AttributesTable();
            foreach (var kv in _edges.Attributes) {
                attr.Add(kv.key, kv.value);
            }

            var shape = _edges.Shape.ToList();
            var coors = new Coordinate[shape.Count()];

            for (int i = 0; i < shape.Count(); i++) {
                var shp = shape[i];
                coors[i] = new Coordinate(shp.longitude, shp.latitude);
            }
            
            this.Current = new Feature(
                new LineString(coors), attr);
            
            return true;
        }

        public void Reset()
        {
            _verticesAreDepleted = false;
            _edges.Reset();
            _vertexIds.Reset();
        }

        public IFeature Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
            _vertexIds.Dispose();
        }
    }
}