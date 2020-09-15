using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all edges that have at least one vertex in a tile range.
    /// </summary>
    internal class EdgeEnumerator
    {
        private readonly GraphEdgeEnumerator _graphGraphEdgeEnumerator;
        private readonly IEnumerator<VertexId> _vertexEnumerator;

        public EdgeEnumerator(Graph graph, IEnumerable<VertexId> vertices)
        {
            _vertexEnumerator = vertices.GetEnumerator();
            _graphGraphEdgeEnumerator = graph.GetEdgeEnumerator();
        }
        
        private bool _firstEdge = false;
        
        public void Reset()
        {
            _firstEdge = false;
            _graphGraphEdgeEnumerator.Reset();
            _vertexEnumerator.Reset();
        }

        public bool MoveNext()
        {
            if (!_firstEdge)
            {
                while (_vertexEnumerator.MoveNext())
                {
                    while (_graphGraphEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                    {
                        if (!_graphGraphEdgeEnumerator.MoveNext()) break;

                        _firstEdge = true;
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                if (_graphGraphEdgeEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext()) return false;
                while (_graphGraphEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                {
                    if (_graphGraphEdgeEnumerator.MoveNext()) return true;
                    if (!_vertexEnumerator.MoveNext()) return false;
                }
            }
        }

        internal GraphEdgeEnumerator GraphGraphEdgeEnumerator => _graphGraphEdgeEnumerator;

        public void Dispose()
        {
            
        }
    }
}