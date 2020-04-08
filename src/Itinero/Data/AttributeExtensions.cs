using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Data
{
    /// <summary>
    /// Contains extension methods for attribute handling.
    /// </summary>
    public static class AttributeExtensions
    {
        /// <summary>
        /// Tries to get the value for the given key.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value, if any.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        public static bool TryGetValue(this IEnumerable<(string key, string value)> attributes, string key, out string value)
        {
            if (attributes == null)
            {
                value = string.Empty;
                return false;
            }
            
            foreach (var (k, v) in attributes)
            {
                if (key != k) continue;
                
                value = v;
                return true;
            }

            value = string.Empty;
            return false;
        }

        /// <summary>
        /// Returns true if the given attribute collection contains the same attributes than the given collection.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="other">The other attributes.</param>
        /// <param name="exclude">Keys to exclude.</param>
        /// <returns>Trie of the same attributes.</returns>
        public static bool ContainsSame(this IEnumerable<(string key, string value)> attributes,
            IEnumerable<(string key, string value)> other, params string[] exclude)
        {
            var attributesCount = 0;
            var otherCount = 0;
            if (attributes != null)
            {
                foreach (var a in attributes)
                {
                    if (!exclude.Contains(a.key))
                    {
                        attributesCount++;
                        if (!other.Contains(a))
                        {
                            return false;
                        }
                    }
                }
            }

            if (other != null)
            {
                foreach (var a in other)
                {
                    if (!exclude.Contains(a.key))
                    {
                        otherCount++;
                        if (!attributes.Contains(a))
                        {
                            return false;
                        }
                    }
                }
            }
            return attributesCount == otherCount;
        }

        /// <summary>
        /// Removes the attribute with the given key.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="key">The key.</param>
        /// <returns>True if the key was found.</returns>
        public static bool RemoveKey(this List<(string key, string value)> attributes, string key)
        {
            if (attributes == null) return false;
            
            for (var i = 0; i < attributes.Count; i++)
            {
                var a = attributes[i];
                if (a.key != key) continue;

                attributes.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds or replaces the value for the given attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="other">The other attributes.</param>
        public static void AddOrReplace(this List<(string key, string value)> attributes,
            IEnumerable<(string key, string value)> other)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (other == null) return;

            foreach (var a in attributes)
            {
                attributes.AddOrReplace(a.key, a.value);
            }
        }

        /// <summary>
        /// Adds or replaces the value for the given key.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if the key was found.</returns>
        public static bool AddOrReplace(this List<(string key, string value)> attributes,
            string key, string value)
        {
            if (attributes == null) return false;
            
            for (var i = 0; i < attributes.Count; i++)
            {
                var a = attributes[i];
                if (a.key != key) continue;

                attributes[i] = (key, value);
                return true;
            }

            return false;
        }
    }
}