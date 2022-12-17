using System;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests;

internal class SnappingTest : FunctionalTest<SnapPoint, (RoutingNetwork routerDb, double longitude, double latitude,
    Profile profile)>
{
    protected override async Task<SnapPoint> ExecuteAsync(
        (RoutingNetwork routerDb, double longitude, double latitude, Profile profile) input)
    {
        var result = await input.routerDb.Snap(input.profile).ToAsync((input.longitude, input.latitude, null));

        if (result.IsError)
        {
            throw new Exception($"SnapPoint test failed: could not snap to point ({input.longitude}, {input.latitude}) because {result.ErrorMessage}");
        }

        return result.Value;
    }

    public static readonly SnappingTest Default = new();
}
