namespace Itinero.Routers
{
    /// <summary>
    /// Abstract representation of a router that has a source.
    /// </summary>
    public interface IHasTarget : IRouter
    {
        /// <summary>
        /// Gets the target.
        /// </summary>
        (SnapPoint sp, bool? direction) Target { get; }
    }
}