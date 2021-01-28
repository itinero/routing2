using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;

namespace Itinero.IO.Streams.Processors
{
    /// <summary>
    ///     A PreprocessorStream decorates another OsmStreamSource.
    ///     Every feature is passed into the PreprocessFeature-method (which is abstract and handled by the children).
    /// </summary>
    public abstract class PreprocessorStream : OsmStreamSource
    {
        private readonly OsmStreamSource _actualSource;

        /// <summary>
        ///     Indicates how much passes this preprocessor need to be fully applied.
        /// </summary>
        /// <example>
        ///     A preprocessor adding tags to members a relation will need two passes: one to build an index of all relations
        ///     (and their respective members), a second to actually apply tags
        /// </example>
        public readonly int NeededPasses;
        

        protected PreprocessorStream(OsmStreamSource actualSource, int neededPasses)
        {
            _actualSource = actualSource;
            NeededPasses = neededPasses;
        }

        public override bool CanReset => _actualSource.CanReset;

        /// <summary>
        /// This method is called just after the moveNext on the actual source stream
        /// </summary>
        /// <param name="feature">The feature to preprocess, identical to actualSource.Current() (which equals this.Current())</param>
        private protected abstract void PreprocessFeature(OsmGeo feature);
        
        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            var moved = _actualSource.MoveNext(ignoreNodes, ignoreWays, ignoreRelations);
            if (moved) {
                this.PreprocessFeature(_actualSource.Current());
            }
            return moved;
        }

        public override OsmGeo Current()
        {
            return _actualSource.Current();
        }

        public override void Reset()
        {
            _actualSource.Reset();
        }
    }
}