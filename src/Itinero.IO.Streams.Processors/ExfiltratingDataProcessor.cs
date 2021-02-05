using System;
using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Streams.Processors
{
    public class ExfiltratingDataProcessor : PreprocessorStream
    {
        private readonly HashSet<string> _data = new();
        private readonly Func<OsmGeo, string?> _extract;
        private readonly Action<HashSet<string>> _whenDone;
        private bool _extracted = false;

        /// <summary>
        ///     Extract data from an OsmGeo-object and stores it in a set
        /// </summary>
        /// <param name="actualSource"></param>
        /// <param name="extract">
        ///     A function calculating a value to keep track of. Can return null or an empty string if no
        ///     relevant value is shown
        /// </param>
        /// <param name="whenDone">When the stream resets, this action is called</param>
        public ExfiltratingDataProcessor(OsmStreamSource actualSource,
            Func<OsmGeo, string> extract,
            Action<HashSet<string>> whenDone
        ) : base(actualSource, 1)
        {
            _extract = extract;
            _whenDone = whenDone;
        }

        private protected override void PreprocessFeature(OsmGeo feature)
        {
            var value = _extract(feature);
            if (string.IsNullOrEmpty(value)) {
                return;
            }

            _data.Add(value);
        }

        public override void Reset()
        {
            base.Reset();
            if (!_extracted) {
                _whenDone(_data);
                _extracted = true;
            }
        }
    }
}