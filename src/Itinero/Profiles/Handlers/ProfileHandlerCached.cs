using System;
using System.Collections.Generic;
using Itinero.Data.Edges;

namespace Itinero.Profiles.Handlers
{
    internal class ProfileHandlerCached : ProfileHandler
    {
        private readonly ProfileHandler _profileHandler;
        private readonly Func<uint, IEnumerable<(string key, string value)>> _getEdgeProfile;
        private EdgeFactor?[] _cache;
        
        public ProfileHandlerCached(ProfileHandler profileHandler, Func<uint, IEnumerable<(string key, string value)>> getEdgeProfile)
        {
            _profileHandler = profileHandler;
            _getEdgeProfile = getEdgeProfile;
            _cache = new EdgeFactor?[1024];
        }

        private EdgeFactor _edgeFactor = EdgeFactor.NoFactor;
        private uint _length;

        public override void MoveTo(RouterDbEdgeEnumerator enumerator)
        {
            var edgeProfileId = enumerator.Enumerator.EdgeProfileId;
            _length = (uint) (enumerator.EdgeLength() * 100);
            if (edgeProfileId == null)
            {
                _profileHandler.MoveTo(enumerator);
                _edgeFactor = _profileHandler.EdgeFactor;
                return;
            }

            while (_cache.Length < edgeProfileId.Value)
            {
                Array.Resize(ref _cache, _cache.Length + 1024);
            }

            var edgeFactor = _cache[edgeProfileId.Value];
            if (edgeFactor == null)
            {
                _profileHandler.MoveTo(enumerator);
                edgeFactor = _profileHandler.EdgeFactor;
                _cache[edgeProfileId.Value] = edgeFactor;
            }

            _edgeFactor = edgeFactor.Value;
        }

        public override uint Length => _length;
        public override EdgeFactor EdgeFactor => _edgeFactor;
    }
}