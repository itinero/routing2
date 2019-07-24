using System.IO;
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

        /// <summary>
        /// Sets the given router db, the data provider loads data into the given router db.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        void SetRouterDb(RouterDb routerDb);

        /// <summary>
        /// Writes the state of the data provider to the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The number of byte written.</returns>
        long WriteTo(Stream stream);

        /// <summary>
        /// Reads the state of the data provider from the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        void ReadFrom(Stream stream);
    }
}