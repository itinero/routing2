using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Reading;

namespace Itinero.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all edges that have at least one vertex in a tile range.
    /// </summary>
    internal class EdgeEnumerator : IGraphEdgeEnumerator<Graph>
    {
        private readonly IEnumerator<VertexId> _vertexEnumerator;

        public EdgeEnumerator(Graph graph, IEnumerable<VertexId> vertices)
        {
            Graph = graph;
            _vertexEnumerator = vertices.GetEnumerator();
            GraphGraphEdgeEnumerator = graph.GetEdgeEnumerator();
        }
        
        private bool _firstEdge = false;
        
        public void Reset()
        {
            _firstEdge = false;
            GraphGraphEdgeEnumerator.Reset();
            _vertexEnumerator.Reset();
        }

        public bool MoveNext()
        {
            if (!_firstEdge)
            {
                while (_vertexEnumerator.MoveNext())
                {
                    while (GraphGraphEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                    {
                        if (!GraphGraphEdgeEnumerator.MoveNext()) break;

                        _firstEdge = true;
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                if (GraphGraphEdgeEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext()) return false;
                while (GraphGraphEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                {
                    if (GraphGraphEdgeEnumerator.MoveNext()) return true;
                    if (!_vertexEnumerator.MoveNext()) return false;
                }
            }
        }

        internal GraphEdgeEnumerator<Graph> GraphGraphEdgeEnumerator { get; }

        public void Dispose()
        {
            
        }

        public Graph Graph { get; }

        public bool Forward => GraphGraphEdgeEnumerator.Forward;
        public VertexId From => GraphGraphEdgeEnumerator.From;
        public (double longitude, double latitude) FromLocation => GraphGraphEdgeEnumerator.FromLocation;
        public VertexId To => GraphGraphEdgeEnumerator.To;
        public (double longitude, double latitude) ToLocation => GraphGraphEdgeEnumerator.ToLocation;
        public EdgeId Id => GraphGraphEdgeEnumerator.Id;
        public IEnumerable<(double longitude, double latitude)> Shape => GraphGraphEdgeEnumerator.Shape;
        public IEnumerable<(string key, string value)> Attributes => GraphGraphEdgeEnumerator.Attributes;
        public uint? EdgeTypeId => GraphGraphEdgeEnumerator.EdgeTypeId;
        public uint? Length => GraphGraphEdgeEnumerator.Length;
        public byte? Head => GraphGraphEdgeEnumerator.Head;
        public byte? Tail => GraphGraphEdgeEnumerator.Tail;
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder)
        {
            return GraphGraphEdgeEnumerator.GetTurnCostTo(fromOrder);
        }

        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder)
        {
            return GraphGraphEdgeEnumerator.GetTurnCostFrom(toOrder);
        }
    }
}