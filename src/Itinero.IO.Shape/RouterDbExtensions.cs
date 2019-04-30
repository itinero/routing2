using System.Collections.Generic;
using System.IO;
using System.Threading;
using NetTopologySuite;
using NetTopologySuite.IO;

namespace Itinero.IO.Shape
{
    /// <summary>
    /// Holds extensions to the router db.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Writes the routerdb to a shapefile.
        /// </summary>
        public static void WriteToShape(this RouterDb routerDb, string fileName)
        {
            var writer = new Writer.ShapeFileWriter(routerDb, fileName);
            writer.Run(CancellationToken.None);
        }
    }
}