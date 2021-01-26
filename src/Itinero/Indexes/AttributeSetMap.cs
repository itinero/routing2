using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Indexes
{
    internal class AttributeSetMap
    {
        protected AttributeSetMap()
        {
            Id = 0;
            Mapping = a => Enumerable.Empty<(string key, string value)>();
        }

        public int Id { get; protected set; }

        public Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> Mapping {
            get;
            protected set;
        }

        public static readonly AttributeSetMap Default = new();
    }
}