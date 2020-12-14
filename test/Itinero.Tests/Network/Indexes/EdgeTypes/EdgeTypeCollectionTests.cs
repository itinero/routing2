using System.IO;
using System.Linq;
using Itinero.Network.Indexes.EdgeTypes;
using Xunit;

namespace Itinero.Tests.Network.Indexes.EdgeTypes
{
    public class EdgeTypeCollectionTests
    {
        [Fact]
        public void GraphEdgeTypeCollection_Get_First_ShouldAdd()
        {
            var edgeTypes = new EdgeTypeCollection();

            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypeCollection_Get_Second_ShouldAdd()
        {
            var edgeTypes = new EdgeTypeCollection();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "primary")});
            Assert.Equal(1U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "primary"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypeCollection_Get_SecondIdentical_ShouldGet()
        {
            var edgeTypes = new EdgeTypeCollection();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypeCollection_Get_SecondIdentical_OrderShouldNotMatter_ShouldGet()
        {
            var edgeTypes = new EdgeTypeCollection();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential"), ("maxspeed", "50")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("maxspeed", "50"), ("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Equal(2, edgeTypeAttributes.Length);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
            Assert.Equal(("maxspeed", "50"), edgeTypeAttributes[1]);
        }

        [Fact]
        public void GraphEdgeTypeCollection_Serialize_Empty_ShouldDeserializeEmpty()
        {
            var expected = new EdgeTypeCollection();
            
            var stream = new MemoryStream();
            expected.Serialize(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var edgeTypes = EdgeTypeCollection.Deserialize(stream);
            Assert.Equal(0U, edgeTypes.Count);
        }

        [Fact]
        public void GraphEdgeTypeCollection_Serialize_One_ShouldDeserializeOne()
        {
            var expected = new EdgeTypeCollection();
            var type1 = expected.Get(new (string key, string value)[] {("highway", "residential")});
            
            var stream = new MemoryStream();
            expected.Serialize(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var edgeTypes = EdgeTypeCollection.Deserialize(stream);
            Assert.Equal(1U, edgeTypes.Count);
            var edgeType1 = edgeTypes.GetById(type1).ToArray();
            Assert.Equal(("highway", "residential"), edgeType1[0]);
        }

        [Fact]
        public void GraphEdgeTypeCollection_Serialize_Two_ShouldDeserializeTwo()
        {
            var expected = new EdgeTypeCollection();
            var type1 = expected.Get(new (string key, string value)[] {("highway", "residential")});
            var type2 = expected.Get(new (string key, string value)[] {("highway", "primary"), ("maxspeed", "50")});
            
            var stream = new MemoryStream();
            expected.Serialize(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var edgeTypes = EdgeTypeCollection.Deserialize(stream);
            Assert.Equal(2U, edgeTypes.Count);
            var edgeType1 = edgeTypes.GetById(type1).ToArray();
            Assert.Equal(("highway", "residential"), edgeType1[0]);
            var edgeType2 = edgeTypes.GetById(type2).ToArray();
            Assert.Equal(("highway", "primary"), edgeType2[0]);
            Assert.Equal(("maxspeed", "50"), edgeType2[1]);
        }
    }
}