using System.Collections.Generic;
using System.Linq;
using Itinero.Collections;
using Itinero.Data.Graphs;
using Itinero.Logging;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Osm
{
    public class RouterDbStreamTarget : OsmStreamTarget
    {
        private readonly Dictionary<long, VertexId> _vertexPerNode;
        private readonly SparseLongIndex _potentialRoutingNodes;
        private readonly HashSet<long> _routingNodes;
        private readonly Graph _graph;

        public RouterDbStreamTarget(Graph graph)
        {
            _graph = graph;
            
            _vertexPerNode = new Dictionary<long, VertexId>();
            _routingNodes = new HashSet<long>();
            _potentialRoutingNodes = new SparseLongIndex(); 
        }
        
        private bool _firstPass = true;
        
        public override void Initialize()
        {
            _firstPass = true;
        }

        public override bool OnBeforePull()
        {
            // execute the first pass.
            this.DoPull(true, false, false);
            
            // move to second pass.
            _firstPass = false;
            this.Source.Reset();
            this.DoPull();

            return false;
        }

        public override void AddNode(Node node)
        {
            if (_firstPass) return;

            if (!node.Id.HasValue) return;
            if (!node.Longitude.HasValue || !node.Latitude.HasValue) return;
            if (!_routingNodes.Contains(node.Id.Value)) return;

            var vertex = _graph.AddVertex(node.Longitude.Value, node.Latitude.Value);
            _vertexPerNode[node.Id.Value] = vertex;
            _routingNodes.Remove(node.Id.Value);
        }

        public override void AddWay(Way way)
        {
            if (_firstPass)
            { // keep track of nodes that are used as routing nodes.
                if (!way.Tags.ContainsKey("highway")) return;

                _routingNodes.Add(way.Nodes[0]);
                if (way.Nodes.Length <= 1) return;
                _routingNodes.Add(way.Nodes[way.Nodes.Length - 1]);
                for (var n = 1; n < way.Nodes.Length - 1; n++)
                {
                    if (_potentialRoutingNodes.Contains(way.Nodes[n]))
                    {
                        _routingNodes.Add(way.Nodes[n]);
                        continue;
                    }
                    _potentialRoutingNodes.Add(way.Nodes[n]);
                }
            }
            else
            {
                if (!way.Tags.ContainsKey("highway")) return;

                var vertex1 = VertexId.Empty;
                for (var n = 0; n < way.Nodes.Length; n++)
                {
                    if (!_vertexPerNode.TryGetValue(way.Nodes[n], out var vertex2))
                    {
                        continue;
                    }

                    if (vertex1.IsEmpty())
                    {
                        vertex1 = vertex2;
                        continue;
                    }

                    var edgeId = _graph.AddEdge(vertex1, vertex2);
                    //Logging.Logger.Log(nameof(RouterDbStreamTarget), TraceEventType.Verbose, 
                    //    $"New edge {edgeId} added: {vertex1}->{vertex2}");
                    vertex1 = vertex2;
                }
            }
        }

        public override void AddRelation(Relation relation)
        {
            // TODO: reimplement turn-restriction support.
        }
    }
}