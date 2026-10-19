using System.Collections.Generic;
using Itinero.Indexes;
using System.Runtime.CompilerServices;
using System.Threading;
using Itinero.Data.Providers;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero;

public sealed partial class RouterDb
{
    /// <summary>
    /// Gets the provider this db reads its types and external data from.
    /// </summary>
    internal DataProviderBase Provider { get; }

    /// <summary>
    /// Gets the edge types, id to attributes.
    /// </summary>
    /// <remarks>
    /// Immutable for this db's life and append-only: an existing id's attributes never change,
    /// which is what makes an id mean the same thing in every tile and every network.
    /// </remarks>
    internal IAttributeSetIndex EdgeTypeIndex { get; }

    /// <summary>
    /// Gets the turn cost types, id to attributes.
    /// </summary>
    internal IAttributeSetIndex TurnCostTypeIndex { get; }

    // Keyed on the profile instance, not its name: Profile is open and a LuaProfile is built from a
    // script, so two different profiles can share a name and would then share factors silently.
    private readonly ConditionalWeakTable<Profile, ProfileCostFunction> _costFunctions = new();

    private long _memoisedProfiles;

    /// <summary>
    /// Gets the cost function for the given profile, memoising the factor per edge type id.
    /// </summary>
    /// <remarks>
    /// Here rather than on a network: an edge type id never changes meaning, so a factor stays
    /// correct across every network this db publishes. Weakly keyed, so a profile minted for one
    /// request takes its memo with it.
    /// </remarks>
    internal ProfileCostFunction GetCostFunction(Profile profile)
    {
        return _costFunctions.GetValue(profile, p =>
        {
            Interlocked.Increment(ref _memoisedProfiles);

            return new ProfileCostFunction(p, this.EdgeTypeIndex, this.TurnCostTypeIndex);
        });
    }

    /// <summary>
    /// How many distinct profiles this db has memoised factors for.
    /// </summary>
    /// <remarks>
    /// Keying on the instance means a caller that builds a profile per request gets a cold memo
    /// every request, which is silently slow. This counts them, so the cliff is a number.
    /// </remarks>
    public long MemoisedProfiles => Interlocked.Read(ref _memoisedProfiles);
}
