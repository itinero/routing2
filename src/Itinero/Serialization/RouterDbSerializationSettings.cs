namespace Itinero.Serialization
{
    /// <summary>
    /// Router db serialization settings.
    /// </summary>
    public class RouterDbSerializationSettings
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the tiled flag.
        /// </summary>
        public bool Tiled { get; set; } = false;
    }
}