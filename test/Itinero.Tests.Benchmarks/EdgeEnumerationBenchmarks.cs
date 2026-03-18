using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles.Lua.Osm;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Benchmarks for edge enumeration — the innermost hot loop during routing.
/// Measures MoveNext() throughput which is dominated by varint decoding
/// through the MemoryArray abstraction.
/// </summary>
[MemoryDiagnoser]
public class EdgeEnumerationBenchmarks
{
    private RoutingNetwork _network = null!;
    private RoutingNetworkEdgeEnumerator _enumerator = null!;
    private VertexId[] _vertices = null!;

    [Params(50, 100)]
    public int GridSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var routerDb = NetworkHelper.BuildGridNetwork(this.GridSize, this.GridSize);
        _network = routerDb.Latest;
        _enumerator = _network.GetEdgeEnumerator();

        // collect all vertex ids for iteration.
        var vertices = new List<VertexId>();
        var vertEnum = _network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            vertices.Add(vertEnum.Current);
        }

        _vertices = vertices.ToArray();
    }

    [Benchmark]
    public int EnumerateAllEdges()
    {
        var count = 0;
        foreach (var vertex in _vertices)
        {
            _enumerator.MoveTo(vertex);
            while (_enumerator.MoveNext())
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int EnumerateAllEdges_ReadProperties()
    {
        var count = 0;
        uint lengthSum = 0;
        foreach (var vertex in _vertices)
        {
            _enumerator.MoveTo(vertex);
            while (_enumerator.MoveNext())
            {
                // access properties the Dijkstra hot loop uses.
                _ = _enumerator.EdgeId;
                _ = _enumerator.Head;
                _ = _enumerator.Forward;
                _ = _enumerator.EdgeTypeId;
                lengthSum += _enumerator.Length ?? 0;
                _ = _enumerator.TailOrder;
                _ = _enumerator.HeadOrder;
                count++;
            }
        }

        return count + (int)lengthSum;
    }
}
