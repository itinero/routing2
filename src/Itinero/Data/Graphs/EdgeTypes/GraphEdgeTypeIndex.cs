using System.Collections.Generic;

namespace Itinero.Data.Graphs.EdgeTypes
{
    /// <summary>
    /// A graph type index.
    ///
    /// This has a:
    /// - A function to determine edge type.
    /// - A graph edge type collection.
    /// </summary>
    internal class GraphEdgeTypeIndex
    {
        private readonly GraphEdgeTypeFunc _func;
        private readonly GraphEdgeTypeCollection _edgeTypes;

        public GraphEdgeTypeIndex()
        {
            _func = new GraphEdgeTypeFunc();
            _edgeTypes = new GraphEdgeTypeCollection();
        }

        public int Id => _func.Id;

        public GraphEdgeTypeFunc Func => _func;

        private GraphEdgeTypeIndex(GraphEdgeTypeCollection edgeTypes, GraphEdgeTypeFunc func)
        {
            _edgeTypes = edgeTypes;
            _func = func;
        }

        public uint Get(IEnumerable<(string key, string value)> attributes)
        {
            var edgeType = _func.ToEdgeType(attributes);
            return _edgeTypes.Get(edgeType);
        }

        public IEnumerable<(string key, string value)> GetById(uint edgeTypeId)
        {
            return _edgeTypes.GetById(edgeTypeId);
        }

        public GraphEdgeTypeIndex Next(GraphEdgeTypeFunc func)
        {
            return new GraphEdgeTypeIndex(_edgeTypes, func);
        }
    }
}