namespace Itinero.Routers
{
    /// <summary>
    /// Abstract representation of a router.
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// Gets the network.
        /// </summary>
        Network Network { get; }
        
        /// <summary>
        /// Gets the settings.
        /// </summary>
        RoutingSettings Settings { get; }
    }
}