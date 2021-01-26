using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Network.Attributes;

namespace Itinero {
    public sealed partial class RouterDb {
        /// <summary>
        /// Gets or sets the meta-data attributes list.
        /// </summary>
        public List<(string key, string value)> Meta { get; set; } = new();

        private IEnumerable<(string key, string value)> ReadAttributesFrom(Stream stream) {
            var ver = stream.ReadByte();
            if (ver == 0) {
                return Enumerable.Empty<(string key, string value)>();
            }

            return stream.ReadAttributesFrom();
        }

        private void WriteAttributesTo(Stream stream) {
            stream.WriteByte(1);

            Meta.WriteAttributesTo(stream);
        }
    }
}