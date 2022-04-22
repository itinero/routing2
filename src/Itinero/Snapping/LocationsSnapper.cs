using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search;
using Itinero.Profiles;

namespace Itinero.Snapping;

/// <summary>
/// Just like the `Snapper`, it'll snap to a location.
/// However, the 'Snapper' will match to any road whereas the `LocationSnapper` will only snap to roads accessible to the selected profiles
/// </summary>
internal class LocationsSnapper : ILocationsSnapper
{
    private readonly Snapper _snapper;
    private readonly IEnumerable<Profile> _profiles;

    public LocationsSnapper(Snapper snapper, IEnumerable<Profile> profiles)
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
        if (!hasProfiles) {
            return (_) => true;
        }

        return (eEnum) => {
            var allOk = true;

            foreach (var costFunction in costFunctions) {
                var costs = costFunction.Get(eEnum, true,
                    Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

                var profileIsOk = costs.canAccess &&
                                  (!CheckCanStopOn || costs.canStop);

                if (AnyProfile && profileIsOk) {
                    return true;
                }

                allOk = profileIsOk && allOk;
            }

            return allOk;
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Result<SnapPoint>> ToAsync(IEnumerable<(double longitude, double latitude, float? e)> locations,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // We need to give 'AcceptableFunc' as function pointer later on
        // We construct this function only once
        var acceptableFunc = AcceptableFunc();

        foreach (var location in locations) {
            // calculate search box.
            var box = location.BoxAround(MaxOffsetInMeter);

            // make sure data is loaded.
            await _snapper.RoutingNetwork.RouterDb.UsageNotifier.NotifyBox(_snapper.RoutingNetwork, box, cancellationToken);
                
            // break when cancelled.
            if (cancellationToken.IsCancellationRequested) break;
 
            // snap to closest edge.
            var snapPoint = _snapper.RoutingNetwork.SnapInBox(box, acceptableFunc);
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