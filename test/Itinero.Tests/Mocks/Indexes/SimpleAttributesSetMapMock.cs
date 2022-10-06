using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Indexes;

namespace Itinero.Tests.Mocks.Indexes;

internal class SimpleAttributesSetMapMock : AttributeSetMap
{
    public SimpleAttributesSetMapMock()
        : base(Guid.NewGuid())
    {
    }

    public override IEnumerable<(string key, string value)> Map(IEnumerable<(string key, string value)> attributes)
    {
        return attributes.ToList();
    }
}
