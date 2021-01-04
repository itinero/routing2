using Itinero.Indexes;
using Xunit;

namespace Itinero.Tests.Indexes
{
    public class AttributeSetMapTests
    {
        [Fact]
        public void AttributeSetMap_Default_ShouldReturnEmpty()
        {
            var attributeSet = AttributeSetMap.Default;
            
            Assert.Equal(0, attributeSet.Id);
            Assert.Empty(attributeSet.Mapping(new (string key, string value)[0]));
        }
    }
}