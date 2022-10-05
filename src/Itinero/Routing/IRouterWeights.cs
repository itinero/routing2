namespace Itinero.Routing;

/// <summary>
/// Abstract representation of a router configured for weight calculations.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRouterWeights<out T>
    where T : IRouter
{
    /// <summary>
    /// Gets the router.
    /// </summary>
    T Router { get; }
}

internal class RouterWeights<T> : IRouterWeights<T>
    where T : IRouter
{
    public RouterWeights(T router)
    {
        this.Router = router;
    }

    public T Router { get; }
}
