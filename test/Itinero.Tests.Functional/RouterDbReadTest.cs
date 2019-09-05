using System.IO;
using Itinero.IO.Osm.Tiles;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A router db reading test.
    /// </summary>
    public class RouterDbReadTest : FunctionalTest<RouterDb, string>
    {
        protected override RouterDb Execute(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return RouterDb.ReadFrom(stream, new DataProvider());
            }
        }
        
        /// <summary>
        /// The default router db reading test.
        /// </summary>
        public static readonly RouterDbReadTest Default = new RouterDbReadTest();
    }
}