using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Geo.Elevation;
using Itinero.IO.Osm.Restrictions.Barriers;
using Itinero.IO.Osm.Restrictions.Turns;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles.Standalone.Global;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Osm;

/// <inheritdoc />
public class RouterDbStreamTarget : OsmStreamTarget
{
    private readonly Dictionary<long, VertexId> _vertices = new();
    private readonly RoutingNetworkMutator _mutableRouterDb;
    private readonly IElevationHandler? _elevationHandler;
    private readonly OsmTurnRestrictionParser _restrictionParser = new();
    private readonly Dictionary<long, Way?> _restrictionMembers = new();
    private readonly OsmBarrierParser _barrierParser = new();
    private readonly Dictionary<long, List<Way>> _barrierNodes = new();
    private readonly Dictionary<long, (double longitude, double latitude)> _nodeLocations = new();
    private readonly HashSet<long> _usedNodes = new();
    private readonly Dictionary<GlobalEdgeId, EdgeId> _globalEdgeIds = new();

    /// <inheritdoc />
    public RouterDbStreamTarget(RoutingNetworkMutator mutableRouterDb,
        IElevationHandler? elevationHandler = null)
    {
        _mutableRouterDb = mutableRouterDb;
        _elevationHandler = elevationHandler;
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

        // add barriers as turn costs after all edges exist.
        foreach (var (nodeId, ways) in _barrierNodes)
        {
            if (!_nodeLocations.ContainsKey(nodeId)) continue;
            var node = new Node { Id = nodeId };
            // find the original node to get tags - check if it's a barrier.
            if (!_barrierParser.TryParse(node, ways, out var barrier)) continue;

            this.ResolveAndAddTurnCosts(barrier.ToGlobalNetworkRestrictions());
        }

        return false;
    }

    /// <inheritdoc />
    public override void AddNode(Node node)
    {
        if (!node.Id.HasValue) return;
        if (!node.Longitude.HasValue || !node.Latitude.HasValue) return;

        if (_firstPass)
        {
            // FIRST PASS: detect barrier nodes.
            if (_barrierParser.IsBarrier(node))
            {
                _vertices[node.Id.Value] = VertexId.Empty;
                _barrierNodes[node.Id.Value] = [];
            }
            return;
        }

        // SECOND PASS: keep node locations.
        _nodeLocations[node.Id.Value] = (node.Longitude.Value, node.Latitude.Value);
        if (!_vertices.TryGetValue(node.Id.Value, out _)) return;

        // store barrier node with tags for later parsing.
        if (_barrierNodes.ContainsKey(node.Id.Value))
        {
            // replace the placeholder with the actual node that has tags.
            _barrierNodes[node.Id.Value] = _barrierNodes[node.Id.Value];
        }
    }

    /// <inheritdoc />
    public override void AddWay(Way way)
    {
        if (way.Nodes == null || way.Nodes.Length == 0 || !way.Id.HasValue) return;

        if (_firstPass)
        {
            // FIRST PASS: keep track of nodes that are used as routing nodes.
            _vertices[way.Nodes[0]] = VertexId.Empty;
            for (var i = 0; i < way.Nodes.Length; i++)
            {
                var node = way.Nodes[i];

                // track ways for barrier nodes.
                if (_barrierNodes.TryGetValue(node, out var barrierWays))
                {
                    barrierWays.Add(way);
                }

                if (_usedNodes.Contains(node))
                {
                    _vertices[node] = VertexId.Empty;
                    continue;
                }

                _usedNodes.Add(node);
            }

            _vertices[way.Nodes[^1]] = VertexId.Empty;
            return;
        }

        // SECOND PASS: add edges and register GlobalEdgeIds.

        // if way is a member of restriction, queue for later.
        var saveEdge = _restrictionMembers.ContainsKey(way.Id.Value);
        if (saveEdge) _restrictionMembers[way.Id.Value] = way;

        // process the way into edges.
        var vertex1 = VertexId.Empty;
        var vertex1Idx = -1;
        var shape = new List<(double longitude, double latitude, float? e)>();
        for (var n = 0; n < way.Nodes.Length; n++)
        {
            var node = way.Nodes[n];
            if (!_nodeLocations.TryGetValue(node, out var location))
            {
                // an incomplete way, node not in source.
                break;
            }

            if (!_vertices.TryGetValue(node, out var vertex2))
            {
                // node is shape.
                var coordinate = location.AddElevation(
                    elevationHandler: _elevationHandler);
                shape.Add(coordinate);
                continue;
            }

            if (vertex2.IsEmpty())
            {
                // node is core and not present yet.
                var coordinate = location.AddElevation(
                    elevationHandler: _elevationHandler);
                vertex2 = _mutableRouterDb.AddVertex(coordinate);
                _vertices[node] = vertex2;
            }

            if (vertex1.IsEmpty())
            {
                vertex1 = vertex2;
                vertex1Idx = n;
                continue;
            }

            // add edge.
            var filteredTags = way.Tags?.Select(x => (x.Key, x.Value));
            var edgeId = _mutableRouterDb.AddEdge(vertex1, vertex2,
                shape,
                filteredTags);

            // register GlobalEdgeId → EdgeId for restriction resolution.
            if (saveEdge)
            {
                var globalEdgeId = way.CreateGlobalEdgeId(vertex1Idx, n);
                _globalEdgeIds[globalEdgeId] = edgeId;
            }

            // move to next part.
            vertex1 = vertex2;
            vertex1Idx = n;
            shape.Clear();
        }
    }

    /// <inheritdoc />
    public override void AddRelation(Relation relation)
    {
        if (relation.Members == null || relation.Members.Length == 0) return;
        if (_firstPass)
        {
            if (!_restrictionParser.IsRestriction(relation, out _)) return;

            // log member ways.
            foreach (var relationMember in relation.Members)
            {
                if (relationMember.Type != OsmGeoType.Way) continue;

                _restrictionMembers[relationMember.Id] = null;
            }
            return;
        }

        // try to parse restriction.
        var result = _restrictionParser.TryParse(relation,
            k => !_restrictionMembers.TryGetValue(k, out var osmGeo) ? null : osmGeo,
            out var restriction);

        if (result.IsError) return;
        if (!result.Value) return;
        if (restriction == null)
            throw new Exception("restriction parsing was successful but restriction is null");

        this.ResolveAndAddTurnCosts(restriction.ToGlobalNetworkRestrictions());
    }

    private void ResolveAndAddTurnCosts(IEnumerable<GlobalRestriction> globalRestrictions)
    {
        var enumerator = _mutableRouterDb.GetEdgeEnumerator();
        foreach (var globalRestriction in globalRestrictions)
        {
            if (!globalRestriction.TryBuildNetworkRestriction(GetEdge, out var networkRestriction))
                continue;

            if (networkRestriction!.Count < 2) continue;

            // get last edge and turn cost vertex.
            var last = networkRestriction[^1];
            if (!enumerator.MoveTo(last.edge, last.forward)) continue;
            var turnCostVertex = enumerator.Tail;

            var secondToLast = networkRestriction[^2];
            if (networkRestriction.IsProhibitory)
            {
                var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
                _mutableRouterDb.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                    new[] { secondToLast.edge, last.edge }, costs,
                    networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
            }
            else
            {
                if (!enumerator.MoveTo(secondToLast.edge, secondToLast.forward)) continue;
                var to = enumerator.Head;
                enumerator.MoveTo(to);

                while (enumerator.MoveNext())
                {
                    if (enumerator.EdgeId == secondToLast.edge ||
                        enumerator.EdgeId == last.edge) continue;

                    var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
                    _mutableRouterDb.AddTurnCosts(turnCostVertex, networkRestriction.Attributes,
                        new[] { secondToLast.edge, enumerator.EdgeId }, costs,
                        networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
                }
            }
        }

        return;

        (EdgeId edge, bool forward)? GetEdge(GlobalEdgeId geid)
        {
            if (_globalEdgeIds.TryGetValue(geid, out var edgeId))
                return (edgeId, true);
            if (_globalEdgeIds.TryGetValue(geid.GetInverted(), out edgeId))
                return (edgeId, false);
            return null;
        }
    }
}
