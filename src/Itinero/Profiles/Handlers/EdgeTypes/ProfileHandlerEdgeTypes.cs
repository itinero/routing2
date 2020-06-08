namespace Itinero.Profiles.Handlers.EdgeTypes
{
    internal class ProfileHandlerEdgeTypes : ProfileHandler
    {
        private readonly ProfileHandlerDefault _profileHandler;
        private readonly ProfileHandlerEdgeTypesCache _profileHandlerEdgeTypesCache;

        public ProfileHandlerEdgeTypes(ProfileHandlerDefault profileHandler, ProfileHandlerEdgeTypesCache profileHandlerEdgeTypesCache)
        {
            _profileHandler = profileHandler;
            _profileHandlerEdgeTypesCache = profileHandlerEdgeTypesCache;
        }

        private EdgeFactor _edgeFactor;
        private uint _length;

        public override void MoveTo(NetworkEdgeEnumerator enumerator)
        {
            var edgeTypeId = enumerator.EdgeTypeId;
            if (edgeTypeId == null)
            {
                _profileHandler.MoveTo(enumerator);
                _edgeFactor = _profileHandler.EdgeFactor;
                return;
            }
            
            var edgeFactor = _profileHandlerEdgeTypesCache.Get(edgeTypeId.Value);
            if (edgeFactor == null)
            {
                _profileHandler.MoveTo(enumerator);
                _edgeFactor = _profileHandler.EdgeFactor;
                _profileHandlerEdgeTypesCache.Set(edgeTypeId.Value, _edgeFactor);
            }
            else
            {
                _edgeFactor = edgeFactor.Value;
            }
            
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

        public override uint ForwardWeight => _edgeFactor.ForwardFactor * _length;
        public override uint BackwardWeight => _edgeFactor.ForwardFactor * _length;
        public override uint ForwardSpeed  => _edgeFactor.ForwardSpeed;
        public override uint BackwardSpeed => _edgeFactor.BackwardSpeed;
        public override bool CanStop  => _edgeFactor.CanStop;
    }
}