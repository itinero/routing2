using System;

namespace Itinero.Instructions.Instructions
{
    /// <summary>
    ///     An instruction is a piece that describes a traveller how they should behave to reach their destination.
    ///     An instruction applies on one or more segments, and contains two parts:
    ///     - The next meters to travel, e.g. 'follow along road XYZ, which has properties P and Q'
    ///     - What to do at the end of the segment, e.g. 'turn left' or even 'follow along then next road'
    ///     An instruction thus can be
    ///     - 'Follow along road XYZ, then turn right'
    ///     - 'Follow road XYZ, then cross street ABC'
    ///     - 'Follow road XYZ, then get onto the roundabout'
    ///     - 'Take the Nth exit of the roundabout, this is ~straight on' (note that this instruction possibly spans multiple
    ///     segments of the roundabout)
    ///     A client which outputs actual text for humans, might even need a little bit more of lookahead in the instructions,
    ///     in order to generate stuff as:
    ///     - 'Follow road XYZ, then cross the street into DEF'.
    ///     This is _not_ handled here
    ///     This means we have two things to track:
    ///     - The properties of the road segment we are travelling
    ///     - The next maneuver to make
    ///     Subclasses of Instruction might add additional information, such as 'cross XYZ at the end', 'you are currently
    ///     travelling a roundabout', ...
    ///     Note that we should also consider the multiple presentations of the instructions:
    ///     - Voice instructions, which should be spoken at the right time before the traveller has to make a maneuver
    ///     - Written instructions, on app, which should show the appropriate text at the right time
    ///     - Extra information, e.g. about the street currently travelled, the next street, the upcoming turn, ...
    /// </summary>
    public class BaseInstruction
    {
        /// <summary>
        ///     The index of the start of the segment
        /// </summary>
        public readonly int ShapeIndex;

        /// <summary>
        ///     The index where the described instruction stops.
        ///     <remarks>
        ///         Will often (but not always) be ShapeIndex + 1. The start- and endinstruction have ShapeIndexEnd ==
        ///         ShapeIndex; some others describe multiple segments
        ///     </remarks>
        /// </summary>
        public readonly int ShapeIndexEnd;

        /// <summary>
        ///     The amount of degrees to turn at the end of the road.
        ///     0° is straight on, positive is turning left and negative is turning right
        /// </summary>
        public readonly int TurnDegrees;

        public readonly string Type;


        public BaseInstruction(int shapeIndex, int shapeIndexEnd, int turnDegrees)
        {
            ShapeIndex = shapeIndex;
            ShapeIndexEnd = shapeIndexEnd;
            TurnDegrees = turnDegrees;
            Type = Tp(this);
        }

        public BaseInstruction(int shapeIndex, double turnDegrees)
        {
            ShapeIndex = shapeIndex;
            ShapeIndexEnd = shapeIndex + 1;
            TurnDegrees = turnDegrees.NormalizeDegrees();
            Type = Tp(this);
        }

        private static string Tp(object o)
        {
            var tp = o.GetType().Name.ToLower();
            return tp.EndsWith("instruction") ? tp.Substring(0, tp.Length - "instruction".Length) : tp;
        }

        public override string ToString()
        {
            return $"Follow from p{ShapeIndex} to p{ShapeIndexEnd}, where you turn {TurnDegrees}°";
        }
    }

    public class BaseInstructionConstructor : IInstructionConstructor
    {
        public string Name => "BaseInstruction";

        public BaseInstruction Construct(IndexedRoute route, int offset, out int usedInstructions)
        {
            if (offset >= route.Shape.Count - 1)
            {
                usedInstructions = 0;
                return null;
            }

            usedInstructions = 1;

            (double, double) nextPoint;
            if (route.Shape.Count - 2 == offset)
                nextPoint = route.Route.Stops[route.Route.Stops.Count - 1].Coordinate;
            else
                nextPoint = route.Shape[offset + 2];

            var nextDirection = Utils.AngleBetween(route.Shape[offset + 1], nextPoint);
            var currentDirection = Utils.AngleBetween(route.Shape[offset], route.Shape[offset + 1]);
            var instruction = new BaseInstruction(offset, nextDirection - currentDirection);

            return instruction;
        }
    }


    /*
     * Ideas for 'low hanging fruit':
     * Go over the bridge
     * Go through the tunnel
     *     => (how does this combine with underground crossroads, e.g. beneath koekelberg? Should we ignore small culvert bridges?
     *
     * Higher hanging fruit:
     *     This is a cyclestreet - cyclists should not be overtaken by cars
     *     Go left/right after the traffic lights
     *     Go right at the intersection (which has a speed table)
     *     Follow the blue network until you see the red network
     *     ...
     */
}