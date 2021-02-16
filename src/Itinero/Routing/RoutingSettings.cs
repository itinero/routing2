using Itinero.Profiles;
using Itinero.Routes.Builders;

namespace Itinero.Routing
{
    /// <summary>
    /// Settings to configure routing.
    /// </summary>
    public class RoutingSettings
    {
        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// Gets or sets the route builder.
        /// </summary>
        public IRouteBuilder RouteBuilder { get; set; } = Routes.Builders.RouteBuilder.Default;

        /// <summary>
        /// Caps a search until the given distance in meter.
        /// </summary> 
        public double MaxDistance { get; set; } = double.MaxValue;
    }
}