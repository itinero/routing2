using System;
using Itinero.Data.Graphs.Coders;

namespace Itinero.Profiles.Handlers
{
    /// <summary>
    /// The default profile handler, no caching, no optimizations.
    /// </summary>
    internal class ProfileInlineUInt32WeightHandlerDefault : ProfileHandler
    {
        private readonly Profile _profile;
        private readonly EdgeDataCoderUInt32 _coder;

        public ProfileInlineUInt32WeightHandlerDefault(Profile profile, 
            EdgeDataCoderUInt32 coder)
        {
            _profile = profile;
            _coder = coder;
        }

        private EdgeFactor? _edgeFactor = null;
        private uint? _weightForward;
        private uint? _weightBackward;
        private uint? _length;
        private RouterDbEdgeEnumerator _enumerator = null;

        public override void MoveTo(RouterDbEdgeEnumerator enumerator)
        {
            // reset all the things.
            _enumerator = enumerator;
            _weightForward = null;
            _weightBackward = null;
            _edgeFactor = null;
            _length = null;
            
            if (_coder.Get(enumerator.Enumerator, out var weight))
            { // get from the storage.
                if (weight >= (1 << 26))
                {
                    _weightForward = weight - (1 << 26);
                    _weightBackward = 0;
                }
                else if (weight >= (1 << 25))
                {
                    _weightForward = 0;
                    _weightBackward = weight - (1 << 25);
                }
                else
                {
                    _weightForward = weight;
                    _weightBackward = weight;
                }
            }
            else
            { // get the factor from the profile and store it.
                this.ExtractFromProfileAndStore();
            }
            
            // reverse if it needs reversing.
            if (!_enumerator.Forward)
            {
                var t = _weightBackward;
                _weightBackward = _weightForward;
                _weightForward = t;
            }
        }

        private void ExtractFromProfileAndStore()
        {
            if (_edgeFactor != null) return;

            var attributes = _enumerator.Attributes;
            _edgeFactor = _profile.Factor(attributes);

            _length = _enumerator.EdgeLength();
            _weightForward = 0;
            _weightBackward = 0;

            // WARNING: always store forward and reverse when we have the actual direction, the edge won't always be accessed in the same direction.
            if (_edgeFactor.Value.ForwardFactor != _edgeFactor.Value.BackwardFactor)
            {
                if (_edgeFactor.Value.ForwardFactor == 0)
                {
                    _weightBackward = _edgeFactor.Value.BackwardFactor * _length;
                    _coder.Set(_enumerator.Enumerator, _weightBackward.Value + (1 << 25));
                    return;
                }
                else if (_edgeFactor.Value.BackwardFactor == 0)
                {
                    _weightForward = _edgeFactor.Value.ForwardFactor * _length;
                    _coder.Set(_enumerator.Enumerator, _weightForward.Value + (1 << 26));
                    return;
                }
                else
                {
                    Console.WriteLine($"Cannot store weight for {_enumerator.Attributes}");
                    return;
                }
            }
            
            _weightForward = _edgeFactor.Value.ForwardFactor * _length;
            _weightBackward = _weightForward;
            if (_weightForward > (1 << 24))
            {
                Console.WriteLine($"Cannot store weight for {_enumerator.Attributes}: > {1 << 24}");
                return;
            }

            _coder.Set(_enumerator.Enumerator, _weightForward.Value);
        }

        public override uint ForwardWeight
        {
            get
            {
                if (_weightForward.HasValue) return _weightForward.Value;
                
                this.ExtractFromProfileAndStore();
                return _edgeFactor.Value.ForwardFactor * _length.Value;
            }
        }
        
        public override uint BackwardWeight
        {
            get
            {
                if (_weightBackward.HasValue) return _weightBackward.Value;
                
                this.ExtractFromProfileAndStore();
                return _edgeFactor.Value.BackwardFactor * _length.Value;
            }
        }
        
        public override uint ForwardSpeed
        {
            get
            {
                this.ExtractFromProfileAndStore();
                return _edgeFactor.Value.ForwardSpeed;
            }
        }
        
        public override uint BackwardSpeed
        {
            get
            {
                this.ExtractFromProfileAndStore();
                return _edgeFactor.Value.BackwardSpeed;
            }
        }
        
        public override bool CanStop
        {
            get
            {
                this.ExtractFromProfileAndStore();
                return _edgeFactor.Value.CanStop;
            }
        }
    }
}