using NUnit.Framework;
using System;

namespace Itinero.Tiled.Tests
{
    [TestFixture]
    public class VertexTileTests
    {
        /// <summary>
        /// Tests the global id functions.
        /// </summary>
        [Test]
        public void TestGlobalId()
        {
            uint localId = 8990789;
            uint tileId = 890267864;

            var id = VertexTile.BuildGlobalId(tileId, localId);
            uint newLocalId, newTileId;
            VertexTile.ExtractLocalId(id, out newTileId, out newLocalId);

            Assert.AreEqual(localId, newLocalId);
            Assert.AreEqual(tileId, newTileId);
        }
    }
}
