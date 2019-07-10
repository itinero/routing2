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
            _enumerator = enumerator;
            _weightForward = null;
            _weightBackward = null;
            _edgeFactor = null;
            _length = null;
            if (_coder.Get(enumerator.Enumerator, out var weight))
            {
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
                return;
            }
            
            this.ExtractFromProfile();
        }

        private void ExtractFromProfile()
        {
            if (_edgeFactor != null) return;
            
            var attributes = _enumerator.GetAttributes();
            _edgeFactor = _profile.Factor(attributes);

            _length = _enumerator.EdgeLength();
            _weightForward = 0;
            _weightBackward = 0;

            if (_edgeFactor.Value.FactorForward != _edgeFactor.Value.FactorBackward)
            {
                if (_edgeFactor.Value.FactorForward == 0)
                {
                    _weightBackward = _edgeFactor.Value.FactorBackward * _length;
                    _coder.Set(_enumerator.Enumerator, _weightBackward.Value + (1 << 25));
                    return;
                }
                else if (_edgeFactor.Value.FactorBackward == 0)
                {
                    _weightForward = _edgeFactor.Value.FactorForward * _length;
                    _coder.Set(_enumerator.Enumerator, _weightForward.Value + (1 << 26));
                    return;
                }
                else
                {
                    Console.WriteLine($"Cannot store weight for {attributes}");
                    return;
                }
            }
            
            _weightForward = _edgeFactor.Value.FactorForward * _length;
            _weightBackward = _weightForward;
            if (_weightForward > (1 << 24))
            {
                Console.WriteLine($"Cannot store weight for {attributes}: > {1 << 24}");
                return;
            }

            _coder.Set(_enumerator.Enumerator, _weightForward.Value);
        }

        public override uint ForwardWeight
        {
            get
            {
                if (_weightForward.HasValue) return _weightForward.Value;
                
                this.ExtractFromProfile();
                return _edgeFactor.Value.FactorForward * _length.Value;
            }
        }
        
        public override uint BackwardWeight
        {
            get
            {
                if (_weightBackward.HasValue) return _weightBackward.Value;
                
                this.ExtractFromProfile();
                return _edgeFactor.Value.FactorBackward * _length.Value;
            }
        }
        
        public override uint ForwardSpeed
        {
            get
            {
                this.ExtractFromProfile();
                return _edgeFactor.Value.ForwardSpeed;
            }
        }
        
        public override uint BackwardSpeed
        {
            get
            {
                this.ExtractFromProfile();
                return _edgeFactor.Value.BackwardSpeed;
            }
        }
        
        public override bool CanStop
        {
            get
            {
                this.ExtractFromProfile();
                return _edgeFactor.Value.CanStop;
            }
        }
    }
}