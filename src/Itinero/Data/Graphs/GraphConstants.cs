namespace Itinero.Data.Graphs
{
    public static class GraphConstants
    {
        /// <summary>
        /// A status to indicate that the tile was not loaded.
        /// </summary>
        public const uint TileNotLoaded = uint.MaxValue;

        /// <summary>
        /// A status to indicate that the tile is empty.
        /// </summary>
        public const uint TileEmpty = uint.MaxValue - 1;

        /// <summary>
        /// A status to indicate there is no vertex set.
        /// </summary>
        public const uint NoVertex = uint.MaxValue;

        /// <summary>
        /// A status to indicate there are no edge associated with this vertex.
        /// </summary>
        public const uint NoEdges = uint.MaxValue - 1;
    }
}

