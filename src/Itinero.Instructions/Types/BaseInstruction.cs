namespace Itinero.Instructions.Types
{
    /// <summary>
    /// A base instruction.
    /// </summary>
    /// <remarks>
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
    /// </remarks>
    internal class BaseInstruction
    {
        public BaseInstruction(IndexedRoute route,
            int shapeIndex, int shapeIndexEnd, int turnDegrees)
        {
            this.Route = route;
            this.ShapeIndex = shapeIndex;
            this.ShapeIndexEnd = shapeIndexEnd;
            this.TurnDegrees = turnDegrees;
            this.Type = Tp(this);
        }

        public BaseInstruction(IndexedRoute route, int shapeIndex, double turnDegrees)
        {
            this.Route = route;
            this.ShapeIndex = shapeIndex;
            this.ShapeIndexEnd = shapeIndex + 1;
            this.TurnDegrees = turnDegrees.NormalizeDegrees();
            this.Type = Tp(this);
        }
        
        /// <summary>
        ///     The index of the start of the segment this instruction is applicable on; i.e. the traveller arrived at the segment
        ///     which starts at 'ShapeIndex', what should they do next?
        /// </summary>
        public int ShapeIndex { get; }

        /// <summary>
        ///     The index where the described instruction stops.
        ///     <remarks>
        ///         Will often (but not always) be ShapeIndex + 1. The start- and endinstruction have ShapeIndexEnd ==
        ///         ShapeIndex; some others describe multiple segments
        ///     </remarks>
        /// </summary>
        public int ShapeIndexEnd { get; }

        /// <summary>
        ///     The amount of degrees to turn at the end of the road.
        ///     0° is straight on, positive is turning left and negative is turning right
        /// </summary>
        public int TurnDegrees { get; }

        /// <summary>
        /// Gets the type of instruction.
        /// </summary>
        public string Type  { get; }

        public IndexedRoute Route {
            get; /* Important - because this is a property, it'll won't be picked up in the substitutions because that one only loads fields */
        }

        private static string Tp(object o)
        {
            var tp = o.GetType().Name.ToLower();
            return tp.EndsWith("instruction") ? tp.Substring(0, tp.Length - "instruction".Length) : tp;
        }

        public override string ToString()
        {
            return $"Follow from p{this.ShapeIndex} to p{this.ShapeIndexEnd}, where you turn {this.TurnDegrees}°";
        }
    }
}