using Itinero.Profiles;

namespace Itinero
{
    /// <summary>
    /// Settings to configure routing.
    /// </summary>
    public class RoutingSettings
    {
        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        public Profile Profile { get; set; }

        /// <summary>
        /// Caps a search until the given distance in meter.
        /// </summary> 
        public double MaxDistance { get; set; } = double.MaxValue;
    }
}