using System.Collections.Generic;
using System.Linq;
using Itinero.Geo.Elevation;
using Itinero.IO.Osm.Collections;
using Itinero.IO.Osm.Filters;
using Itinero.IO.Osm.Restrictions;
using Itinero.Logging;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Mutation;
using OsmSharp;
using OsmSharp.Db;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Itinero.IO.Osm
{
    public class RouterDbStreamTarget : OsmStreamTarget
    {
        private readonly Dictionary<long, VertexId> _vertexPerNode;
        private readonly NodeIndex _nodeIndex;
        private readonly RoutingNetworkMutator _mutableRouterDb;
        private readonly RoutingNetworkMutatorEdgeEnumerator _mutableRouterDbEdgeEnumerator;
        private readonly ITagsFilter _tagsFilter;

        public RouterDbStreamTarget(RoutingNetworkMutator mutableRouterDb,
            ITagsFilter tagsFilter)
        {
            _mutableRouterDb = mutableRouterDb;
            _tagsFilter = tagsFilter;

            _mutableRouterDbEdgeEnumerator = _mutableRouterDb.GetEdgeEnumerator();
            
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
            var filteredTags = _tagsFilter.Filter(way);
            if (filteredTags == null) return;
            
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
                // check if the way is part of a restriction.
                // if yes, keep it.
                var osmGeoKey = new OsmGeoKey(way);
                List<EdgeId>? edgesList = null;
                if (_restrictionWayMembers.ContainsKey(osmGeoKey)) edgesList = new List<EdgeId>(1);
                
                var vertex1 = VertexId.Empty;
                var shape = new List<(double longitude, double latitude, float? e)>();
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
                        
                        // add elevation.
                        var coordinate = ((double)longitude, (double)latitude).AddElevation(null);
                        
                        if (!isCore)
                        { // node is just a shape point, keep it but don't add is as a vertex.
                            shape.Add(coordinate);
                            continue;
                        }
                        else
                        { // node is a core vertex, add it as a vertex.
                            vertex2 = _mutableRouterDb.AddVertex(coordinate);
                            _vertexPerNode[node] = vertex2;
                        }
                    }
                    
                    if (vertex1.IsEmpty())
                    {
                        vertex1 = vertex2;
                        continue;
                    }

                    var edgeId = _mutableRouterDb.AddEdge(vertex1, vertex2,
                        shape: shape,
                        attributes: filteredTags);
                    vertex1 = vertex2;
                    shape.Clear();

                    edgesList?.Add(edgeId);
                }

                if (edgesList != null) _restrictionWayMembers[osmGeoKey] = edgesList;
            }
        }
        
        private readonly Dictionary<OsmGeoKey, IEnumerable<EdgeId>?> _restrictionWayMembers = new Dictionary<OsmGeoKey, IEnumerable<EdgeId>?>();

        public override void AddRelation(Relation relation)
        {
            var negativeResult = relation.IsNegative();
            if (negativeResult.IsError) return; // not a valid restriction.
            var negative = negativeResult.Value;
            
            // this is a restriction.
            if (_firstPass)
            { // keep track of nodes that should definitely be core because
              // they have a turn-restriction at their location.
                foreach (var member in relation.Members)
                {
                    if (member.Type == OsmGeoType.Relation) continue;
                    
                    // index all way members.
                    if (member.Type == OsmGeoType.Way)
                        _restrictionWayMembers[new OsmGeoKey(member.Type, member.Id)] = null;

                    // make nodes as core.
                    if (member.Type != OsmGeoType.Node) continue;
                    if (member.Role != "via") continue;
                    _nodeIndex.AddId(member.Id);
                }
            }
            else
            {
                IEnumerable<(VertexId from, VertexId to, EdgeId id)> EdgesForWay(long wayId)
                {
                    if (_restrictionWayMembers.TryGetValue((new OsmGeoKey(OsmGeoType.Way, wayId)), 
                            out var edges) &&
                        edges != null)
                    {
                        foreach (var edge in edges)
                        {
                            _mutableRouterDbEdgeEnumerator.MoveToEdge(edge);
                            yield return (_mutableRouterDbEdgeEnumerator.From, _mutableRouterDbEdgeEnumerator.To,
                                _mutableRouterDbEdgeEnumerator.Id);
                        }
                    }
                }
                
                // get the restricted sequence.
                var sequenceResult = relation.GetEdgeSequence(n =>
                {
                    if (!_vertexPerNode.TryGetValue(n, out var v)) return null;

                    return v;
                }, EdgesForWay);
                
                // check if the sequence was found.
                if (sequenceResult.IsError)
                {
                    Logger.Log($"{nameof(RouterDbStreamTarget)}.{nameof(AddRelation)}", 
                        TraceEventType.Information, $"Relation {relation} could not be parsed as a restriction: {sequenceResult.ErrorMessage}");
                    return;
                }
                
                // // invert negative sequences.
                // if (negative)
                // {
                //     foreach (var inverted in sequenceResult.Value.Invert(_mutableRouterDbEdgeEnumerator))
                //     {
                //         this.AddRestrictedSequence(inverted, relation.Tags);
                //     }
                // }
                // else
                // {
                //     this.AddRestrictedSequence(sequenceResult.Value, relation.Tags);
                // }
            }
        }

        private void AddRestrictedSequence(IEnumerable<(EdgeId edgeId, bool forward)> sequence, TagsCollectionBase tags)
        {
            using var e = sequence.GetEnumerator();
            if (!e.MoveNext()) return;
            var first = e.Current;
            if (!e.MoveNext()) return;
            var second = e.Current;
            if (e.MoveNext()) return;
            
            // convert to a turn cost table.
            var costs = new uint[,] {{0, 1,}, {0, 0}};
            var edges = new [] { first.edgeId, second.edgeId };
            
            // get the vertex to add the turn cost to.
            _mutableRouterDbEdgeEnumerator.MoveToEdge(first.edgeId, first.forward);
            var vertexId = _mutableRouterDbEdgeEnumerator.To;
            
            // add the turn cost.
            _mutableRouterDb.AddTurnCosts(vertexId, tags.Select(x => (x.Key, x.Value)), edges,
                costs);
        }
    }
}