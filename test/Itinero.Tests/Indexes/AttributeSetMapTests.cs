using System;
using Itinero.Indexes;
using Xunit;

namespace Itinero.Tests.Indexes;

public class AttributeSetMapTests
{
    [Fact]
    public void AttributeSetMap_Default_ShouldReturnEmpty()
    {
        var attributeSet = AttributeSetMap.Default();

        Assert.Equal(Guid.Empty, attributeSet.Id);
        Assert.Empty(attributeSet.Map(Array.Empty<(string key, string value)>()));
    }
}
