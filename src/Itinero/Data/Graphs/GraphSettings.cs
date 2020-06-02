namespace Itinero.Data.Graphs
{
    /// <summary>
    /// The graph settings.
    /// </summary>
    internal class GraphSettings
    {
        /// <summary>
        /// The zoom level.
        /// </summary>
        public int Zoom { get; set; }
        
        /// <summary>
        /// The tile resolution in bytes.
        /// </summary>
        public int TileResolution { get; set; }

        /// <summary>
        /// Gets the default settings.
        /// </summary>
        public static GraphSettings Default => new GraphSettings()
        {
            Zoom = 14,
            TileResolution = 3
        };

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{this.TileResolution}r@z{this.Zoom}";
        }
    }
}