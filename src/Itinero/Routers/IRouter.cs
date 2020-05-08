namespace Itinero.Routers
{
    /// <summary>
    /// Abstract representation of a router.
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// Gets the router db.
        /// </summary>
        RouterDb RouterDb { get; }
        
        /// <summary>
        /// Gets the settings.
        /// </summary>
        RoutingSettings Settings { get; }
    }
}