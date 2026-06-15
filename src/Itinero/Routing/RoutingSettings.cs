using System;
using Itinero.Profiles;
using Itinero.Routes.Builders;
using Itinero.Routing.Costs;

namespace Itinero.Routing;

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

    /// <summary>
    /// An optional hook to wrap the cost function used during routing. When set, the routing
    /// engine invokes this with the profile-derived cost function and uses the returned function
    /// for the search. This is the supported way to inject per-request edge cost adjustments
    /// (e.g. congestion) without mutating the RouterDb.
    /// </summary>
    public Func<ICostFunction, ICostFunction>? CostFunctionWrapper { get; set; }
}
