using Itinero.Network;

namespace Itinero.Routing {
    /// <summary>
    /// Abstract representation of a router.
    /// </summary>
    public interface IRouter {
        /// <summary>
        /// Gets the network.
        /// </summary>
        RoutingNetwork Network { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        RoutingSettings Settings { get; }
    }
}