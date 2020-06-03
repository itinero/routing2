using Itinero.Data.Graphs;
using System.Linq;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphEdgeTypesTests
    {
        [Fact]
        public void GraphEdgeTypes_Get_First_ShouldAdd()
        {
            var edgeTypes = new GraphEdgeTypes();

            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypes_Get_Second_ShouldAdd()
        {
            var edgeTypes = new GraphEdgeTypes();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "primary")});
            Assert.Equal(1U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "primary"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypes_Get_SecondIdentical_ShouldGet()
        {
            var edgeTypes = new GraphEdgeTypes();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Single(edgeTypeAttributes);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
        }
        
        [Fact]
        public void GraphEdgeTypes_Get_SecondIdentical_OrderShouldNotMatter_ShouldGet()
        {
            var edgeTypes = new GraphEdgeTypes();

            edgeTypes.Get(new (string key, string value)[] {("highway", "residential"), ("maxspeed", "50")});
            var edgeTypeId = edgeTypes.Get(new (string key, string value)[] {("maxspeed", "50"), ("highway", "residential")});
            Assert.Equal(0U, edgeTypeId);

            var edgeTypeAttributes = edgeTypes.GetById(edgeTypeId).ToArray();
            Assert.Equal(2, edgeTypeAttributes.Length);
            Assert.Equal(("highway", "residential"), edgeTypeAttributes[0]);
            Assert.Equal(("maxspeed", "50"), edgeTypeAttributes[1]);
        }
    }
}