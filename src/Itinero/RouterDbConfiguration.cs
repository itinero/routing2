using System.IO;
using Itinero.Data;
using Itinero.Data.Graphs.Coders;

namespace Itinero
{
    /// <summary>
    /// Defines router db settings.
    /// </summary>
    public class RouterDbConfiguration
    {
        /// <summary>
        /// Gets or sets the zoom level.
        /// </summary>
        public int Zoom { get; set; }
        
        /// <summary>
        /// Gets or sets the edge data layout.
        /// </summary>
        public EdgeDataLayout EdgeDataLayout { get; set; }
        
        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        public static readonly RouterDbConfiguration Default = new RouterDbConfiguration() { Zoom = 14 };
    }
}