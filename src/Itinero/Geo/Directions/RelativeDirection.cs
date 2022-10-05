namespace Itinero.Geo.Directions
{
    /// <summary>
    /// Represents a relative direction.
    /// </summary>
    public class RelativeDirection
    {
        /// <summary>
        /// Gets or sets the direction
        /// </summary>
        public RelativeDirectionEnum Direction { get; set; }

        /// <summary>
        /// Gets or sets the angle.
        /// </summary>
        public double Angle { get; set; }
    }
}
