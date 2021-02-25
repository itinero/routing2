using System;
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
    internal class RoutingNetworkStream : IEnumerable<IFeature>
    {
        private readonly RoutingNetwork _network;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> _preprocessEdgeAttributes;

        public RoutingNetworkStream(RoutingNetwork network, 
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)> > preprocessEdgeAttributes)
        {
            _network = network;
            _preprocessEdgeAttributes = preprocessEdgeAttributes;
        }

        public IEnumerator<IFeature> GetEnumerator()
        {
            return new RoutingNetworkEnumerator(_network,_preprocessEdgeAttributes);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class RoutingNetworkEnumerator : IEnumerator<IFeature>
    {
        private readonly RoutingNetwork _network;
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> _preprocessEdge;

        private readonly IEnumerator<VertexId> _vertexIds;


        private bool _verticesAreDepleted;
        private RoutingNetworkEdgeEnumerator _edges;

        public RoutingNetworkEnumerator(RoutingNetwork network,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)> > preprocessEdge)
        {
            _network = network;
            _preprocessEdge = preprocessEdge;
            _vertexIds = network.GetVertices().GetEnumerator();
           _edges = network.GetEdgeEnumerator();
        }

        public bool MoveNext()
        {
            _verticesAreDepleted = !_vertexIds.MoveNext();
            if (!_verticesAreDepleted) {
                var attrs = new AttributesTable{
                    {"_vertix_id", "" + _vertexIds.Current}
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

            var attrTable = new AttributesTable();
            var rawAttr = _edges.Attributes;
            if (_preprocessEdge != null) {
                rawAttr = _preprocessEdge.Invoke(rawAttr);
            }
            foreach (var kv in rawAttr) {
                attrTable.Add(kv.key, kv.value);
            }

            var shape = _edges.Shape.ToList();
            var coors = new Coordinate[shape.Count()];

            for (int i = 0; i < shape.Count(); i++) {
                var shp = shape[i];
                coors[i] = new Coordinate(shp.longitude, shp.latitude);
            }
            
            this.Current = new Feature(
                new LineString(coors), attrTable);
            
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