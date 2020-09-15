using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    internal class MutableNetwork : IDisposable
    {
        private readonly Network _network;
        private readonly Graph.MutableGraph _graph;

        public MutableNetwork(Network network)
        {
            _network = network;

            _graph = network.Graph.GetAsMutable();
        }

        internal Graph.MutableGraph Graph => _graph;

        public VertexId AddVertex(double longitude, double latitude)
        {
            return _graph.AddVertex(longitude, latitude);
        }

        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            return _graph.TryGetVertex(vertex, out longitude, out latitude);
        }

        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            return _graph.AddEdge(vertex1, vertex2, shape, attributes);
        }

        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            _graph.AddTurnCosts(vertex, attributes, edges, costs, prefix);
        }

        public void SetEdgeTypeFunc(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            _graph.SetEdgeTypeFunc(_graph.EdgeTypeFunc.NextVersion(func));
        }

        public Network ToNetwork()
        {
            return new Network(_network.RouterDb, _graph.ToGraph());
        }

        public void Dispose()
        {
            _graph.Dispose();

            _network.ClearMutable();
        }
    }
}