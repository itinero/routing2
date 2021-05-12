using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Tests.Network
{
    public class EdgeEnumeratorMock : IEdgeEnumerator<RoutingNetwork>
    {
        private readonly IEnumerator<(EdgeId edge, uint? length, bool forward, uint? edgeTypeId)> _edges;

        public EdgeEnumeratorMock(params EdgeId[] edges)
        {
            this._edges = edges.Select<EdgeId, (EdgeId edge, uint? length, bool forward, uint? edgeTypeId)>(
                x => (x, null, true, null)).GetEnumerator();
        }

        public EdgeEnumeratorMock(params (EdgeId edge, uint? length, bool forward, uint? edgeTypeId)[] edges)
        {
            this._edges = edges.ToList().GetEnumerator();
        }

        public void Reset()
        {
            _edges.Reset();
        }

        public bool MoveNext()
        {
            return _edges.MoveNext();
        }

        public RoutingNetwork Network { get; }
        public bool Forward => _edges.Current.forward;
        public (double longitude, double latitude, float? e) FromLocation { get; }
        public VertexId From { get; }
        public (double longitude, double latitude, float? e) ToLocation { get; }
        public VertexId To { get; }
        public EdgeId Id => _edges.Current.edge;
        public IEnumerable<(double longitude, double latitude, float? e)> Shape { get; }
        public IEnumerable<(string key, string value)> Attributes { get; }
        public uint? EdgeTypeId => _edges.Current.edgeTypeId;
        public uint? Length => _edges.Current.length;
        public byte? Head { get; }
        public byte? Tail { get; }

        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder)
        {
            throw new NotImplementedException();
        }
    }
}