using System;
using System.Collections.Generic;

namespace Itinero.Indexes
{
    internal class AttributeSetMap
    {
        protected AttributeSetMap(int id,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> mapping)
        {
            this.Id = id;
            this.Mapping = mapping;
        }

        public int Id { get; }

        public Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> Mapping { get; }
        
        public static readonly AttributeSetMap Default = new AttributeSetMap(0, a => a);
    }
}