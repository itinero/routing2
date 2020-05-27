using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all edges that have at least one vertex in a tile range.
    /// </summary>
    internal class EdgeEnumerator
    {
        private readonly Graph.Enumerator _graphEnumerator;
        private readonly IEnumerator<VertexId> _vertexEnumerator;

        public EdgeEnumerator(Graph graph, IEnumerable<VertexId> vertices)
        {
            _vertexEnumerator = vertices.GetEnumerator();
            _graphEnumerator = graph.GetEnumerator();
        }
        
        private bool _firstEdge = false;
        
        public void Reset()
        {
            _firstEdge = false;
            _graphEnumerator.Reset();
            _vertexEnumerator.Reset();
        }

        public bool MoveNext()
        {
            if (!_firstEdge)
            {
                while (_vertexEnumerator.MoveNext())
                {
                    while (_graphEnumerator.MoveTo(_vertexEnumerator.Current))
                    {
                        if (!_graphEnumerator.MoveNext()) break;

                        _firstEdge = true;
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                if (_graphEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext()) return false;
                while (_graphEnumerator.MoveTo(_vertexEnumerator.Current))
                {
                    if (_graphEnumerator.MoveNext()) return true;
                    if (!_vertexEnumerator.MoveNext()) return false;
                }
            }
        }

        internal Graph.Enumerator GraphEnumerator => _graphEnumerator;

        public void Dispose()
        {
            
        }
    }
}