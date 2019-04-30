using Itinero.Data.Graphs;

namespace Itinero.Data.Providers
{
    /// <summary>
    /// Abstract representation of a data provider.
    /// </summary>
    public interface ILiveDataProvider
    {
        /// <summary>
        /// Reports to this provider that the given vertex was touched.
        /// </summary>
        /// <param name="vertexId">The vertex id.</param>
        /// <returns>True if this has changed any data.</returns>
        bool TouchVertex(VertexId vertexId);

        /// <summary>
        /// Reports to this provider that all data on the given bbox has to marked as touched.
        /// </summary>
        /// <param name="box">The bbox.</param>
        /// <returns>True if that has changed any data.</returns>
        bool TouchBox((double minLon, double minLat, double maxLon, double maxLat) box);
    }
}