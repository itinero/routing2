using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data.Attributes
{
    /// <summary>
    /// Contains extension methods for attribute collections.
    /// </summary>
    public static class IAttributeCollectionExtension
    {
        /// <summary>
        /// Adds a new attribute.
        /// </summary>
        public static void AddOrReplace(this IAttributeCollection attributes, Attribute attribute)
        {
            attributes.AddOrReplace(attribute.Key, attribute.Value);
        }

        /// <summary>
        /// Adds or replaces the existing attributes to/in the given collection of attributes.
        /// </summary>
        public static void AddOrReplace(this IAttributeCollection attributes, IEnumerable<Attribute> other)
        {
            if (other == null) { return; }

            foreach (var attribute in other)
            {
                attributes.AddOrReplace(attribute.Key, attribute.Value);
            }
        }
        
        /// <summary>
        /// Returns true if the given attribute is found.
        /// </summary>
        public static bool Contains(this IReadonlyAttributeCollection attributes, string key, string value)
        {
            if (!attributes.TryGetValue(key, out var foundValue))
            {
                return false;
            }
            return value == foundValue;
        }
        
        /// <summary>
        /// Returns true if the given attribute collection contains the same attributes than the given collection.
        /// </summary>
        public static bool ContainsSame(this IReadonlyAttributeCollection attributes, IReadonlyAttributeCollection other, params string[] exclude)
        {            
            var attributesCount = 0;
            var otherCount = 0;
            if (attributes != null)
            {
                foreach (var a in attributes)
                {
                    if (exclude.Contains(a.Key)) continue;
                    attributesCount++;
                    if (!other.Contains(a.Key, a.Value))
                    {
                        return false;
                    }
                }
            }

            if (other != null)
            {
                foreach (var a in other)
                {
                    if (exclude.Contains(a.Key)) continue;
                    otherCount++;
                    if (!attributes.Contains(a.Key, a.Value))
                    {
                        return false;
                    }
                }
            }
            return attributesCount == otherCount;
        }
    }
}