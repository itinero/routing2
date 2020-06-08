namespace Itinero.Profiles.Handlers
{
    /// <summary>
    /// The default profile handler, no caching, no optimizations.
    /// </summary>
    internal class ProfileHandlerDefault : ProfileHandler
    {
        private readonly Profile _profile;

        public ProfileHandlerDefault(Profile profile)
        {
            _profile = profile;
        }
        
        internal EdgeFactor EdgeFactor = EdgeFactor.NoFactor;
        private uint _length;

        public override void MoveTo(NetworkEdgeEnumerator enumerator)
        { 
            EdgeFactor = enumerator.FactorInEdgeDirection(_profile);

            var length = enumerator.Length;
            if (length.HasValue)
            {
                _length = length.Value;
            }
            else
            {
                _length = (uint)(enumerator.EdgeLength() * 100);
            }
        }

        public override uint ForwardWeight => EdgeFactor.ForwardFactor * _length;
        public override uint BackwardWeight => EdgeFactor.ForwardFactor * _length;
        public override uint ForwardSpeed  => EdgeFactor.ForwardSpeed;
        public override uint BackwardSpeed => EdgeFactor.BackwardSpeed;
        public override bool CanStop  => EdgeFactor.CanStop;
    }
}