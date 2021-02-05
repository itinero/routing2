using System.Collections.Generic;
using System.IO;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Streams.Processors
{
    public class ExternalDataTagger : PreprocessorStream
    {
        private readonly string _lookupKey;
        private readonly Dictionary<string, string> _values;
        private readonly string _writeToKey;

        /// <summary>
        ///     Matches data from an external dataset onto the features, based on the value of `lookupKey`.
        ///     When this processor has run, the osmGeoObject will satisfy:
        ///     `osmGeo.Tags[writeToKey] = values[osmGeo.Tags[lookupKey]]`
        ///     No tag will be written if anything is missing.
        /// </summary>
        /// <param name="actualSource">The underlying geostream which is amended</param>
        /// <param name="values">The datastore from which extra values will be added</param>
        /// <param name="lookupKey">The _value_ of this key will act as key in `values` and will be added onto the geo-object</param>
        /// <param name="writeToKey">
        ///     The new tagkey of the geo-object, which will receive the value
        ///     `values[object.Tags[lookupKey]]`
        /// </param>
        public ExternalDataTagger(OsmStreamSource actualSource,
            Dictionary<string, string> values,
            string lookupKey,
            string writeToKey
        ) : base(actualSource, 1)
        {
            _lookupKey = lookupKey;
            _writeToKey = writeToKey;
            _values = values;
        }

        public ExternalDataTagger(OsmStreamSource actualSource,
            string pathToCsv,
            string lookupKey,
            string writeToKey
        ) : base(actualSource, 1)
        {
            _lookupKey = lookupKey;
            _writeToKey = writeToKey;
            _values = new Dictionary<string, string>();
            var lines = File.ReadAllLines(pathToCsv);
            foreach (var line in lines) {
                var split = line.Split(",");
                if (split.Length != 2) {
                    continue;
                }

                _values[split[0]] = split[1];
            }
        }

        private protected override void PreprocessFeature(OsmGeo feature)
        {
            var key = feature.Tags?.GetValue(_lookupKey);
            if (string.IsNullOrEmpty(key)) {
                return;
            }

            if (!_values.TryGetValue(key, out var value)) {
                return;
            }

            feature.Tags.AddOrReplace(_writeToKey, value);
        }
    }
}