using System;
using Itinero.Geo;
using Itinero.Instructions.Types.Generators;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.Types
{
    /**
     * A bend in the road is there if:
     * - Within the first 100m, the road turns at least 25°
     * - The road feels continuous:
     * *  All the branches are on the non-turning side (e.g. the turn direction is left, but there are no branches on the left)
     * * OR the branches on the turn-to side are all service roads or tracks and the current one isn't that too
     * - There are no major bends in the other direction (thus: every individual bend is at least 0°)
     */
    internal class FollowBendInstruction : BaseInstruction
    {
        public FollowBendInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
            route, shapeIndex, shapeIndexEnd, turnDegrees) { }
    }
}