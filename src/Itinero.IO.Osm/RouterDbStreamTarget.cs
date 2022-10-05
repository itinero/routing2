using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Geo.Elevation;
using Itinero.IO.Osm.Restrictions;
using Itinero.Network;
using Itinero.Network.Mutation;
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
    private readonly Dictionary<long, (double longitude, double latitude)> _nodeLocations = new();
    private readonly HashSet<long> _usedNodes = new();
    private readonly List<OsmTurnRestriction> _osmTurnRestrictions = new();
    private readonly Dictionary<(long wayId, int node1Idx, int node2Idx), EdgeId> _restrictedEdges = new();

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

        return false;
    }

    /// <inheritdoc />
    public override void AddNode(Node node)
    {
        // FIRST PASS: ignore nodes.
        if (_firstPass) return;

        // SECOND PASS: keep node locations.
        if (!node.Id.HasValue) return;
        if (!node.Longitude.HasValue || !node.Latitude.HasValue) return;
        _nodeLocations[node.Id.Value] = (node.Longitude.Value, node.Latitude.Value);
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

        // SECOND PASS: keep restricted ways and add edges.

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

            // add edges.
            if (way.Id.Value == 27946227) Console.WriteLine("");
            var filteredTags = way.Tags?.Select(x => (x.Key, x.Value));
            var edgeId = _mutableRouterDb.AddEdge(vertex1, vertex2,
                shape,
                filteredTags);

            // check if this edge needs saving for restriction.
            var edgeIdKey = (way.Id.Value, vertex1Idx, n);
            if (saveEdge) _restrictedEdges[edgeIdKey] = edgeId;

            // move to next part.
            vertex1 = vertex2;
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

        var networkRestrictionResult = restriction.ToNetworkRestrictions((long wayId, int startNode, int endNode) =>
        {
            if (startNode < endNode)
            {
                if (!_restrictedEdges.TryGetValue((wayId, startNode, endNode), out var edgeId)) return null;

                return (edgeId, true);
            }
            else
            {
                if (!_restrictedEdges.TryGetValue((wayId, endNode, startNode), out var edgeId)) return null;

                return (edgeId, false);
            }
        });
        if (networkRestrictionResult.IsError) return;

        foreach (var networkRestriction in networkRestrictionResult.Value)
        {
            if (networkRestriction.Count < 2)
            {
                // TODO: log something?
                continue;
            }

            // get last edge and turn cost vertex.
            var last = networkRestriction[^1];
            var lastEdge = _mutableRouterDb.GetEdge(last.edge, last.forward);
            var turnCostVertex = lastEdge.Tail;

            var secondToLast = networkRestriction[^2];
            var costs = new uint[,] { { 0, 1 }, { 0, 0 } };
            _mutableRouterDb.AddTurnCosts(turnCostVertex, networkRestriction.Attributes, new[] { secondToLast.edge, last.edge }, costs,
                networkRestriction.Take(networkRestriction.Count - 2).Select(x => x.edge));
        }
    }
}
