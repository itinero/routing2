using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.Geo;

internal class RoutingNetworkEnumerator : IEnumerator<IFeature>
{
    private readonly RoutingNetwork _network;

    private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>
        _preprocessEdge;

    private readonly IEnumerator<VertexId> _vertexIds;

    private readonly HashSet<EdgeId> _seenVertices = new();


    private readonly RoutingNetworkEdgeEnumerator _edges;

    private bool _edgesAreInitiated;

    public RoutingNetworkEnumerator(RoutingNetwork network,
        Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> preprocessEdge)
    {
        _network = network;
        _preprocessEdge = preprocessEdge;
        _vertexIds = network.GetVertices().GetEnumerator();
        _edges = network.GetEdgeEnumerator();
    }

    public bool MoveNext()
    {
        if (!_edgesAreInitiated)
        {
            var hasNext = _vertexIds.MoveNext();
            if (!hasNext)
            {
                return false;
            }

            _edges.MoveTo(_vertexIds.Current);
            _edgesAreInitiated = true;
            var attrs = new AttributesTable {
                    {"_vertex_id", "" + _vertexIds.Current}
                };
            _network.TryGetVertex(_vertexIds.Current, out var lon, out var lat, out var el);
            this.Current = new Feature(
                new Point(lon, lat, el ?? 0f), attrs);

            return true;
        }

        var hasNextEdge = false;
        do
        {
            hasNextEdge = _edges.MoveNext();
        } while (_seenVertices.Contains(_edges.EdgeId) && hasNextEdge);

        if (!hasNextEdge)
        {
            _edgesAreInitiated = false;
            return this.MoveNext();
        }
        _seenVertices.Add(_edges.EdgeId);


        var attrTable = new AttributesTable();
        var rawAttr = _edges.Attributes;
        if (_preprocessEdge != null)
        {
            rawAttr = _preprocessEdge.Invoke(rawAttr);
        }

        foreach (var kv in rawAttr)
        {
            attrTable.Add(kv.key, kv.value);
        }

        var shape = _edges.Shape.ToList();
        var coors = new Coordinate[shape.Count + 2];
        var (frLon, frLat, _) = _edges.TailLocation;
        coors[0] = new Coordinate(frLon, frLat);
        var (toLon, toLat, _) = _edges.HeadLocation;
        coors[^1] = new Coordinate(toLon, toLat);
        for (var i = 0; i < shape.Count(); i++)
        {
            var shp = shape[i];
            coors[i + 1] = new Coordinate(shp.longitude, shp.latitude);
        }

        this.Current = new Feature(
            new LineString(coors), attrTable);

        return true;
    }

    public void Reset()
    {
        _edges.Reset();
        _vertexIds.Reset();
        _seenVertices.Clear();
    }

    public IFeature Current { get; private set; }

    object IEnumerator.Current => this.Current;

    public void Dispose()
    {
        _vertexIds.Dispose();
    }
}
