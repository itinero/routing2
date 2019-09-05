using System.IO;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A router db writing test.
    /// </summary>
    public class RouterDbWriteTest : FunctionalTest<long, (RouterDb routerDb, string filename)>
    {
        protected override long Execute((RouterDb routerDb, string filename) input)
        {
            using (var stream = File.Open(input.filename, FileMode.Create))
            {
                return input.routerDb.WriteTo(stream);
            }
        }
        
        /// <summary>
        /// The default router db writing test.
        /// </summary>
        public static readonly RouterDbWriteTest Default = new RouterDbWriteTest();
    }
}