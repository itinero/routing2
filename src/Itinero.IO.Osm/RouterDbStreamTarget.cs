using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.IO.Osm.Collections;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Osm
{
    public class RouterDbStreamTarget : OsmStreamTarget
    {
        private readonly Dictionary<long, VertexId> _vertexPerNode;
        private readonly NodeIndex _nodeIndex;
        private readonly RouterDb _routerDb;

        public RouterDbStreamTarget(RouterDb routerDb)
        {
            _routerDb = routerDb;
            
            _vertexPerNode = new Dictionary<long, VertexId>();
            _nodeIndex = new NodeIndex();
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
            _nodeIndex.SortAndConvertIndex();
            this.Source.Reset();
            this.DoPull();

            return false;
        }

        public override void AddNode(Node node)
        {
            if (_firstPass) return;

            if (!node.Id.HasValue) return;
            if (!node.Longitude.HasValue || !node.Latitude.HasValue) return;

            // check if the node is a routing node and if yes, store it's coordinate.
            var index = _nodeIndex.TryGetIndex(node.Id.Value);
            if (index != long.MaxValue)
            { // node is a routing node, store it's coordinates.
                _nodeIndex.SetIndex(index, (float)node.Latitude.Value, (float)node.Longitude.Value);
            }
        }

        public override void AddWay(Way way)
        {
            if (!way.Tags.ContainsKey("highway")) return;
            
            if (_firstPass)
            { // keep track of nodes that are used as routing nodes.

                _nodeIndex.AddId(way.Nodes[0]);
                for (var i = 0; i < way.Nodes.Length; i++)
                {
                    _nodeIndex.AddId(way.Nodes[i]);
                }
                _nodeIndex.AddId(way.Nodes[way.Nodes.Length - 1]);
            }
            else
            {
                var vertex1 = VertexId.Empty;
                var shape = new List<(double longitude, double latitude)>();
                for (var n = 0; n < way.Nodes.Length; n++)
                {
                    var node = way.Nodes[n];
                    if (!_vertexPerNode.TryGetValue(node, out var vertex2))
                    { // no already a vertex, get the coordinates and it's status.
                        if (!_nodeIndex.TryGetValue(node, out var latitude, out var longitude, out var isCore, out _, out _))
                        { // an incomplete way, node not in source.
                            isCore = false;
                            break;
                        }
                        
                        if (!isCore)
                        { // node is just a shape point, keep it but don't add is as a vertex.
                            shape.Add((longitude, latitude));
                            continue;
                        }
                        else
                        { // node is a core vertex, add it as a vertex.
                            vertex2 = _routerDb.AddVertex(longitude, latitude);
                            _vertexPerNode[node] = vertex2;
                        }
                    }
                    
                    if (vertex1.IsEmpty())
                    {
                        vertex1 = vertex2;
                        continue;
                    }

                    _routerDb.AddEdge(vertex1, vertex2,
                        shape: shape,
                        attributes: way.Tags.Select(x => (x.Key, x.Value)));
                    vertex1 = vertex2;
                    shape.Clear();
                }
            }
        }

        public override void AddRelation(Relation relation)
        {
            // TODO: reimplement turn-restriction support.
        }
    }
}