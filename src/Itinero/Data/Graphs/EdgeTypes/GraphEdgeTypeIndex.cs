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

        private GraphEdgeTypeIndex(GraphEdgeTypeCollection edgeTypes, GraphEdgeTypeFunc func)
        {
            _edgeTypes = edgeTypes;
            _func = func;
        }

        public GraphEdgeTypeIndex Next(GraphEdgeTypeFunc func)
        {
            return new GraphEdgeTypeIndex(_edgeTypes, func);
        }
    }
}