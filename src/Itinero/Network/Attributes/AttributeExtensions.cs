using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO;

namespace Itinero.Network.Attributes
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
        public static bool TryGetValue(this IEnumerable<(string key, string value)> attributes, string key,
            out string value)
        {
            foreach (var (k, v) in attributes)
            {
                if (key != k)
                {
                    continue;
                }

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

            foreach (var a in other)
            {
                if (exclude.Contains(a.key))
                {
                    continue;
                }

                otherCount++;
                if (!attributes.Contains(a))
                {
                    return false;
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
            for (var i = 0; i < attributes.Count; i++)
            {
                var a = attributes[i];
                if (a.key != key)
                {
                    continue;
                }

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
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            foreach (var a in other)
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
            for (var i = 0; i < attributes.Count; i++)
            {
                var a = attributes[i];
                if (a.key != key)
                {
                    continue;
                }

                attributes[i] = (key, value);
                return true;
            }

            attributes.Add((key, value));
            return false;
        }

        /// <summary>
        /// Writes the attributes to the given stream starting at the current position of the stream.
        /// </summary>
        /// <param name="attributes">The attributes to write.</param>
        /// <returns>The number of byte written.</returns>
        internal static long WriteAttributesTo(this IEnumerable<(string key, string value)> attributes, Stream stream)
        {
            var pos = stream.Position;
            foreach (var (key, value) in attributes)
            {
                var bytes = System.Text.Encoding.Unicode.GetBytes(key);
                stream.WriteVarInt32(bytes.Length + 1); // 0 is null, end of the attribute set.
                stream.Write(bytes, 0, bytes.Length);

                bytes = System.Text.Encoding.Unicode.GetBytes(value);
                stream.WriteVarInt32(bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            }

            stream.WriteVarInt32(0);

            return stream.Position - pos;
        }

        /// <summary>
        /// Reads the attributes from the given stream starting at the current position of the stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The attributes read.</returns>
        internal static IEnumerable<(string key, string value)> ReadAttributesFrom(this Stream stream)
        {
            var attributes = new List<(string key, string value)>();

            var keySize = stream.ReadVarInt32();
            while (keySize > 0)
            {
                var bytes = new byte[keySize - 1];
                stream.Read(bytes, 0, bytes.Length);
                var key = System.Text.Encoding.Unicode.GetString(bytes);

                var valueSize = stream.ReadVarInt32();
                bytes = new byte[valueSize];
                stream.Read(bytes, 0, bytes.Length);
                var value = System.Text.Encoding.Unicode.GetString(bytes);

                attributes.Add((key, value));

                keySize = stream.ReadVarInt32();
            }

            return attributes;
        }

        public static long GetHash(this IEnumerable<(string key, string value)> attributes)
        {
            var hash = 0;
            foreach (var attribute in attributes)
            {
                hash ^= attribute.GetHashCode();
            }

            return hash;
        }

        public static long GetDiffHash(this (string key, string value) otherValue, long originalHash)
        {
            return originalHash ^ otherValue.GetHashCode();
        }
    }
}
