using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Indexes
{
    internal class AttributeSetMap
    {
        protected AttributeSetMap()
        {
            this.Id = 0;
            this.Mapping = a => Enumerable.Empty<(string key, string value)>();
        }

        public int Id { get; protected set; }

        public Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> Mapping { get; protected set; }

        public static readonly AttributeSetMap Default = new AttributeSetMap();
    }
}