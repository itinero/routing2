using System;
using System.Collections.Generic;
using Itinero.Indexes;

namespace Itinero.Tests.Indexes
{
    internal class AttributeSetMapMock : AttributeSetMap
    {
        private readonly Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> _func;

        public AttributeSetMapMock(Guid id,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
            : base(id)
        {
            _func = func;
        }

        public override IEnumerable<(string key, string value)> Map(IEnumerable<(string key, string value)> attributes)
        {
            return _func(attributes);
        }
    }
}
