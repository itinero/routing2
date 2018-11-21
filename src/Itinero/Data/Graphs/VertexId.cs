namespace Itinero.Data.Graphs
{
    /// <summary>
    /// Represents a vertex ID composed of a tile ID and a vertex ID.
    /// </summary>
    public struct VertexId
    {
        /// <summary>
        /// Gets or sets the tile id.
        /// </summary>
        public uint TileId { get; set; }
        
        /// <summary>
        /// Gets or sets the local id.
        /// </summary>
        public uint LocalId { get; set; }

        /// <summary>
        /// Returns a human readable description.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.LocalId} @ {this.TileId}";
        }
    }
}