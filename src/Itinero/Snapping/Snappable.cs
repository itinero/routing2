using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    internal class Snappable : ISnappable
    {
        private readonly Snapper _snapper;
        private readonly IEnumerable<Profile> _profiles;

        public Snappable(Snapper snapper, IEnumerable<Profile> profiles)
        {
            _snapper = snapper;
            _profiles = profiles;
        }
        
        /// <summary>
        /// A flag to enable the option of using any profile as valid instead of all.
        /// </summary>
        public bool AnyProfile { get; set; } = false;

        /// <summary>
        /// A flag to check the can stop on data.
        /// </summary>
        public bool CheckCanStopOn { get; set; } = true;

        /// <summary>
        /// Gets the maximum offset in meter.
        /// </summary>
        public double MaxOffsetInMeter { get; set; } = 1000;
        
        internal Func<IEdgeEnumerator<RoutingNetwork>, bool> AcceptableFunc()
        {
            var costFunctions = _profiles.Select(_snapper.RoutingNetwork.GetCostFunctionFor).ToArray();
            
            var hasProfiles = costFunctions.Length > 0;
            if (!hasProfiles) return (_) => true;
            
            return (eEnum) =>
            {
                var allOk = true;
                
                foreach (var costFunction in costFunctions)
                {
                    var costs = costFunction.Get(eEnum, true, 
                        Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

                    var profileIsOk = costs.canAccess &&
                                      (!this.CheckCanStopOn || costs.canStop);

                    if (this.AnyProfile && profileIsOk) return true;

                    allOk = profileIsOk && allOk;
                }

                return allOk;
            };
        }
        
        /// <inheritdoc/>
        public IEnumerable<Result<SnapPoint>> To(IEnumerable<(double longitude, double latitude)> locations)
        {
            var acceptableFunc = this.AcceptableFunc();

            foreach (var location in locations)
            {
                // calculate search box.
                var box = location.BoxAround(this.MaxOffsetInMeter);

                // make sure data is loaded.
                _snapper.RoutingNetwork.RouterDb.UsageNotifier?.NotifyBox(_snapper.RoutingNetwork, box);

                // snap to closest edge.
                var snapPoint = _snapper.RoutingNetwork.SnapInBox(box, acceptableFunc);
                if (snapPoint.EdgeId != EdgeId.Empty)
                {
                    yield return snapPoint;
                    
                }
                else
                {
                    yield return new Result<SnapPoint>($"Could not snap to location: {location.longitude},{location.latitude}");
                }
            }
        }
        
        /// <inheritdoc/>
        public IEnumerable<Result<SnapPoint>> To(IEnumerable<(VertexId vertexId, EdgeId? edgeId)> vertices)
        {
            var enumerator = _snapper.RoutingNetwork.GetEdgeEnumerator();

            foreach (var (vertexId, edgeId) in vertices)
            {
                if (!enumerator.MoveTo(vertexId))
                {
                    yield return new Result<SnapPoint>($"Vertex {vertexId} not found.");
                    continue;
                }

                var found = false;
                while (enumerator.MoveNext())
                {
                    if (edgeId != null &&
                        enumerator.Id != edgeId.Value) continue;

                    if (enumerator.Forward)
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.Id, 0));
                    }
                    else
                    {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.Id, ushort.MaxValue));
                    }
                    found = true;
                    break;
                }

                if (found) continue;
                
                if (edgeId.HasValue)
                {
                    yield return new Result<SnapPoint>($"Edge {edgeId.Value} not found for vertex {vertexId}");
                }
                else
                {
                    yield return new Result<SnapPoint>("Cannot snap to a vertex that has no edges.");
                }
            }
        }
    }
}