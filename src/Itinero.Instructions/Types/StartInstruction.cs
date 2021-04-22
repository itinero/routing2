using System;

namespace Itinero.Instructions.Types
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    public class StartInstruction : BaseInstruction
    {
        internal StartInstruction(IndexedRoute route, int turnDegrees, int absoluteStartingDegrees,
            uint projectionDistance, BaseInstruction contained = null) :
            base(route, 0, Math.Max(0, contained?.ShapeIndexEnd ?? 0), turnDegrees)
        {
            this.StartDegrees = absoluteStartingDegrees;
            this.ProjectionDistance = projectionDistance;
            this.Then = contained;
        }


        internal StartInstruction(IndexedRoute route, BaseInstruction contained = null) : this(route,
            0,
            route.DepartingDirectionAt(0).NormalizeDegrees(),
            0, contained) { }

        /// <summary>
        ///     The compass degrees to start the trip, with 0° being 'go to the north'
        /// </summary>
        /// <remarks>
        ///     The 'turnDegrees' is relative to the actual startpoint, thus if walking from the startpoint to the
        ///     snappedpoint, the amount of degrees to turn then
        /// </remarks>

        public int StartDegrees { get; }

        /// <summary>
        ///     The distance between the actual start point and the snapped start point on the road
        /// </summary>
        public uint ProjectionDistance { get; }

        /// <summary>
        ///     The instruction following the start instruction
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public BaseInstruction Then { get; }

        public override string ToString()
        {
            return
                $"Start by going {this.ProjectionDistance}m towards the road, then turn {this.TurnDegrees}° to start a {this.StartDegrees}° journey";
        }
    }
}