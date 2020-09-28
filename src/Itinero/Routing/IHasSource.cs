namespace Itinero.Routing
{
    /// <summary>
    /// Abstract representation of a router that has a source.
    /// </summary>
    public interface IHasSource : IRouter
    {
        /// <summary>
        /// Gets the source.
        /// </summary>
        (SnapPoint sp, bool? direction) Source { get; }
    }
}