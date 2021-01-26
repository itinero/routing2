using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Search;
using Itinero.Profiles;

namespace Itinero.Snapping
{
    internal class Snapper : ISnapper
    {
        public Snapper(RoutingNetwork routingNetwork,
            SnapperSettings? settings = null)
        {
            RoutingNetwork = routingNetwork;
            Settings = settings ?? new SnapperSettings();
        }

        internal RoutingNetwork RoutingNetwork { get; }

        internal SnapperSettings Settings { get; }

        /// <inheritdoc/>
        public ILocationsSnapper Using(Action<SnapperSettings> settings)
        {
            var s = new SnapperSettings();
            settings?.Invoke(s);

            return new Snapper(RoutingNetwork, s);
        }

        /// <inheritdoc/>
        public ILocationsSnapper Using(Profile profile, Action<SnapperSettings>? settings = null)
        {
            var s = new SnapperSettings();
            settings?.Invoke(s);

            return new LocationsSnapper(this, new[] {profile}) {
                AnyProfile = s.AnyProfile,
                CheckCanStopOn = s.CheckCanStopOn,
                MaxOffsetInMeter = s.MaxOffsetInMeter
            };
        }

        /// <inheritdoc/>
        public IEnumerable<Result<SnapPoint>> To(IEnumerable<(VertexId vertexId, EdgeId? edgeId)> vertices)
        {
            var enumerator = RoutingNetwork.GetEdgeEnumerator();

            foreach (var (vertexId, edgeId) in vertices) {
                if (!enumerator.MoveTo(vertexId)) {
                    yield return new Result<SnapPoint>($"Vertex {vertexId} not found.");
                    continue;
                }

                var found = false;
                while (enumerator.MoveNext()) {
                    if (edgeId != null &&
                        enumerator.Id != edgeId.Value) {
                        continue;
                    }

                    if (enumerator.Forward) {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.Id, 0));
                    }
                    else {
                        yield return new Result<SnapPoint>(new SnapPoint(enumerator.Id, ushort.MaxValue));
                    }

                    found = true;
                    break;
                }

                if (found) {
                    continue;
                }

                if (edgeId.HasValue) {
                    yield return new Result<SnapPoint>($"Edge {edgeId.Value} not found for vertex {vertexId}");
                }
                else {
                    yield return new Result<SnapPoint>("Cannot snap to a vertex that has no edges.");
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Result<SnapPoint>> To(IEnumerable<(double longitude, double latitude, float? e)> locations)
        {
            foreach (var location in locations) {
                // calculate search box.
                var box = location.BoxAround(Settings.MaxOffsetInMeter);

                // make sure data is loaded.
                RoutingNetwork.RouterDb.UsageNotifier?.NotifyBox(RoutingNetwork, box);

                // snap to closest edge.
                var snapPoint = RoutingNetwork.SnapInBox(box, (_) => true);
                if (snapPoint.EdgeId != EdgeId.Empty) {
                    yield return snapPoint;
                }
                else {
                    yield return new Result<SnapPoint>(
                        $"Could not snap to location: {location.longitude},{location.latitude}");
                }
            }
        }
    }
}