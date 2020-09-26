using Itinero.Data.Graphs.Reading;

namespace Itinero.Data.Graphs
{
    // TODO: can we do an IEnumerable?
    public interface IGraphEdgeEnumerator<T> : IGraphEdge<T>
        where T : IGraphEdgeEnumerable
    {
        void Reset();
        
        bool MoveNext();
    }
}