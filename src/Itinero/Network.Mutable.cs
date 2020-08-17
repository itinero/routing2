using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    public sealed partial class Network 
    {
        private MutableNetwork? _mutableNetwork = null;

        internal MutableNetwork GetAsMutable()
        {
            if (_mutableNetwork != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
            
            _mutableNetwork = new MutableNetwork(this);
            return _mutableNetwork;
        }

        internal void ClearMutable()
        {
            _mutableNetwork = null;
        }
        
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

            public void SetEdgeTypeFunc(Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
            {
                _graph.SetEdgeTypeFunc(_graph.EdgeTypeFunc.NextVersion(func));
            }

            public uint SetTurnCostFunc(string name, Func<Network, VertexId, uint[]?> turnCostFunc)
            {
                throw new NotImplementedException();
                
                // return _graph.SetTurnCostFunc(name, turnCostFunc);
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
}