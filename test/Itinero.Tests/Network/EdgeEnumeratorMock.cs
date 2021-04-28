using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Tests.Network
{
    public class EdgeEnumeratorMock : IEdgeEnumerator<RoutingNetwork>
    {

        private readonly IEnumerator<EdgeId> _edges;

      public  EdgeEnumeratorMock(params EdgeId[] edges)
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
        public bool Forward { get; }
        public (double longitude, double latitude, float? e) FromLocation { get; }
        public VertexId From { get; }
        public (double longitude, double latitude, float? e) ToLocation { get; }
        public VertexId To { get; }
        public EdgeId Id => _edges.Current;
        public IEnumerable<(double longitude, double latitude, float? e)> Shape { get; }
        public IEnumerable<(string key, string value)> Attributes { get; }
        public uint? EdgeTypeId { get; }
        public uint? Length { get; }
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