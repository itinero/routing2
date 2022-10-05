namespace Itinero.Routing
{
    /// <summary>
    /// Abstract representation of a route with on source but many targets.
    /// </summary>
    public interface IRouterOneToMany : IHasSource, IHasTargets { }
}
