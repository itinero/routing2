using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero.Costs
{
    /// <summary>
    /// Abstract definition of a cost function.
    /// </summary>
    internal interface ICostFunction
    {
        /// <summary>
        /// Moves to the given edge.
        /// </summary>
        /// <param name="edgeEnumerator">The edge enumerator.</param>
        public abstract void MoveTo(NetworkEdgeEnumerator edgeEnumerator);

        /// <summary>
        /// Get the can access flag.
        /// </summary>
        /// <param name="forward">The forward flag, when true the cost in the direction of the current edge enumerator, otherwise against. When null returns true if can stop is true in at least one direction.</param>
        /// <returns>A flag indicating if this edge can be accessed.</returns>
        public abstract bool CanAccess(bool? forward = null);

        /// <summary>
        /// Gets the can stop flag.
        /// </summary>
        /// <param name="forward">The forward flag, when true the cost in the direction of the current edge enumerator, otherwise against. When null returns true if can stop is true in at least one direction.</param>
        /// <returns>A flag indicating if stopping or starting on this edge is allowed.</returns>
        public abstract bool CanStop(bool? forward = null);

        /// <summary>
        /// Gets the cost of the edge selected in edge enumerator.
        /// </summary>
        /// <param name="forward">The forward flag, when true the cost in the direction of the current edge enumerator, otherwise against.</param>
        /// <returns>The edge cost.</returns>
        public abstract double GetCosts(bool forward);

        /// <summary>
        /// Gets the cost of a turn between the current edge and the given edges.
        /// </summary>
        /// <param name="forward">The forward flag, when true the cost is in the direction of the current edge enumerator, otherwise against.</param>
        /// <param name="previousEdges">The previous edges. Should correspond with what the forward flag indicates.</param>
        /// <returns>The cost of the turn.</returns>
        public abstract double GetTurnCost(bool forward, IEnumerable<(EdgeId edgeId, ushort? turn)> previousEdges);
    }
}