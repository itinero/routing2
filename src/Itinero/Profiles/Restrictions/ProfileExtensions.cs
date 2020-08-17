using System;
using Itinero.Data.Graphs;

namespace Itinero.Profiles.Restrictions
{
    internal static class ProfileExtensions
    {
        public static (string name, Func<Network, VertexId, (EdgeId[] edges, uint[] turnCosts)?> costs) GetRestrictedTurnCostFunc(this Profile profile)
        {
            return ($"restrictions-{profile.Name}", (network, vertex) =>
            {
                // get turn restrictions from network around this vertex.
                // if there are none, return (null, null)
            
                // build turn cost table including the restricted turns.
                
                return null;
            });
        }
    }
}