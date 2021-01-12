using System;
using System.Collections.Generic;
using Itinero.Indexes;

namespace Itinero.Tests.Indexes
{
    internal class AttributeSetMapMock : AttributeSetMap
    {
        public AttributeSetMapMock(int id, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
        {
            this.Id = id;
            this.Mapping = func;
        }
    }
}