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

        private EdgeFactor _edgeFactor = EdgeFactor.NoFactor;
        private uint _length;

        public override void MoveTo(RouterDbEdgeEnumerator enumerator)
        {
            _edgeFactor = enumerator.FactorInEdgeDirection(_profile);

            _length = (uint)(enumerator.EdgeLength() * 100);
        }

        public override uint Length => _length;
        public override EdgeFactor EdgeFactor => _edgeFactor;
    }
}