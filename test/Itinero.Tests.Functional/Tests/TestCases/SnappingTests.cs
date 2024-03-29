﻿using System.Linq;
using System.Threading.Tasks;
using Itinero.Profiles;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests.TestCases;

public class SnappingTests
{
    public static async Task RunTestsLux(RouterDb routerDb, Profile profile)
    { // Luxembourg
        var latest = routerDb.Latest;
        var gare = await SnappingTest.Default.RunAsync((latest, 6.13655, 49.59883, profile),
            "Snapping cold: luxembourg gare");
        var schneider = await SnappingTest.Default.RunAsync((latest, 6.03329, 49.63041, profile),
            "Snapping cold: Rue Schneider");
    }

    public static async Task RunTestsNl(RouterDb routerDb, Profile profile)
    {
        var latest = routerDb.Latest;
        var middelburg = await SnappingTest.Default.RunAsync((latest, 3.61363, 51.49967, profile),
            "Snapping cold: middelburg");
    }

    public static async Task RunTestsBe(RouterDb routerDb, Profile profile)
    { // Belgium
        var latest = routerDb.Latest;

        var brugge = routerDb.Latest.Snap(profile).ToAsync(
              new (double longitude, double latitude, float? e)[2] {
                    (3.2203820473368694, 51.215381552063945, 0f),
                    (3.2195755643836605, 51.21651607032328, 0f)
              }
          );

        var stekene = await SnappingTest.Default.RunAsync((latest, 4.03705, 51.20637, profile),
            "Snapping cold: stekene");
        var leuven = await SnappingTest.Default.RunAsync((latest, 4.69575, 50.88040, profile),
            "Snapping cold: leuven");
        var wechelderzande1 = await SnappingTest.Default.RunAsync((latest, 4.80129, 51.26774, profile),
            "Snapping cold: wechelderzande1");
        var wechelderzande2 = await SnappingTest.Default.RunAsync((latest, 4.794577360153198, 51.26723850107129, profile),
            "Snapping cold: wechelderzande2");
        var wechelderzande3 = await SnappingTest.Default.RunAsync((latest, 4.783204793930054, 51.266842437522904, profile),
            "Snapping cold: wechelderzande3");
        var wechelderzande4 = await SnappingTest.Default.RunAsync((latest, 4.796256422996521, 51.261015209797186, profile),
            "Snapping cold: wechelderzande4");
        var wechelderzande5 = await SnappingTest.Default.RunAsync((latest, 4.795172810554504, 51.267413036466706, profile),
            "Snapping cold: wechelderzande5");
        var vorselaar1 = await SnappingTest.Default.RunAsync((latest, 4.7668540477752686, 51.23757128291549, profile),
            "Snapping cold: vorselaar1");
        var hermanTeirlinck = await SnappingTest.Default.RunAsync((latest, 4.35016, 50.86595, profile),
            "Snapping cold: herman teirlinck");
        hermanTeirlinck = await SnappingTest.Default.RunAsync((latest, 4.35016, 50.86595, profile),
            "Snapping hot: hermain teirlinck");
        var mechelenNeckerspoel = await SnappingTest.Default.RunAsync((latest, 4.48991060256958, 51.0298871358546, profile),
            "Snapping cold: mechelen neckerspoel");
        mechelenNeckerspoel = await SnappingTest.Default.RunAsync((latest, 4.48991060256958, 51.0298871358546, profile),
            "Snapping hot: mechelen neckerspoel");
        var dendermonde = await SnappingTest.Default.RunAsync((latest, 4.10142481327057, 51.0227846418863, profile),
            "Snapping cold: dendermonde");
        dendermonde = await SnappingTest.Default.RunAsync((latest, 4.10142481327057, 51.0227846418863, profile),
            "Snapping hot: dendermonde");
        var zellik1 = await SnappingTest.Default.RunAsync((latest, 4.27392840385437, 50.884507285755205, profile),
            "Snapping cold: zellik1");
        zellik1 = await SnappingTest.Default.RunAsync((latest, 4.27392840385437, 50.884507285755205, profile),
            "Snapping hot: zellik1");
        var zellik2 = await SnappingTest.Default.RunAsync((latest, 4.275886416435242, 50.88336336674239, profile),
            "Snapping cold: zellik2");
        zellik2 = await SnappingTest.Default.RunAsync((latest, 4.275886416435242, 50.88336336674239, profile),
            "Snapping hot: zellik2");
        var bruggeStation = await SnappingTest.Default.RunAsync((latest, 3.214899, 51.195129, profile),
            "Snapping cold: brugge-station");
        var stationDuinberge = await SnappingTest.Default.RunAsync((latest, 3.26358318328857, 51.3381990351222, profile),
            "Snapping cold: duinberge");

        await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (_, _) =>
        {
            await SnappingTest.Default.RunAsync((latest, 4.27392840385437, 50.884507285755205, profile),
                "Snapping parallel: zellik1");
            await SnappingTest.Default.RunAsync((latest, 4.275886416435242, 50.88336336674239, profile),
                "Snapping parallel: zellik2");
        });
    }
}
