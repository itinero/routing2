namespace Itinero.Instructions.Instructions {
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
    public class BaseInstruction {
        /// <summary>
        ///     The index of the start of the segment this instruction is applicable on; i.e. the traveller arrived at the segment
        ///     which starts at 'ShapeIndex', what should they do next?
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


        public BaseInstruction(IndexedRoute route,
            int shapeIndex, int shapeIndexEnd, int turnDegrees) {
            Route = route;
            ShapeIndex = shapeIndex;
            ShapeIndexEnd = shapeIndexEnd;
            TurnDegrees = turnDegrees;
            Type = Tp(this);
        }

        public BaseInstruction(IndexedRoute route, int shapeIndex, double turnDegrees) {
            Route = route;
            ShapeIndex = shapeIndex;
            ShapeIndexEnd = shapeIndex + 1;
            TurnDegrees = turnDegrees.NormalizeDegrees();
            Type = Tp(this);
        }

        public IndexedRoute Route {
            get; /* Important - because this is a property, it'll won't be picked up in the substitutions because that one only loads fields */
        }

        private static string Tp(object o) {
            var tp = o.GetType().Name.ToLower();
            return tp.EndsWith("instruction") ? tp.Substring(0, tp.Length - "instruction".Length) : tp;
        }

        public override string ToString() {
            return $"Follow from p{ShapeIndex} to p{ShapeIndexEnd}, where you turn {TurnDegrees}°";
        }
    }

    public class BaseInstructionGenerator : IInstructionGenerator {
        public BaseInstruction Generate(IndexedRoute route, int offset) {
            if (offset == 0) {
                // We are at the very beginning of the route, "turning" as such isn't really defined here
                return null;
            }

            if (offset >= route.Shape.Count - 1) {
                // The current offset is already the last index of the shape; this is the endpoint
                return null;
            }

            return new BaseInstruction(route, offset, route.DirectionChangeAt(offset));
        }
    }
}