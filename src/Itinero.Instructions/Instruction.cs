using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions
{
    /// <summary>
    /// An instruction is an abstract representation for instructions.
    /// It amends a route object, in the sense that a list of instructions is linked via the 'shape'-indices to the route object
    /// 
    /// Translating an 'abstract' instruction into text is done later on (to support e.g. translations).
    /// However, all instructions should provide a decent 'ToString' in order to debug 
    /// </summary>
    public abstract class Instruction
    {
        /// <summary>
        /// The index of a 'route.shape', the starting point for which this instruction is applicable
        /// </summary>
        public readonly uint Shape;

        protected Instruction(uint shape)
        {
            Shape = shape;
        }
    }

    public class AmendedInstruction : Instruction
    {
        public AmendedInstruction(Instruction instruction, params string[] amendments) : base(instruction.Shape)
        {
            Instruction = instruction;
            Amendments = amendments.ToList();
        }
        
        public AmendedInstruction(Instruction instruction, IEnumerable<string> amendments) : base(instruction.Shape)
        {
            Instruction = instruction;
            Amendments = amendments;
        }

        /// <summary>
        /// The type of amendment; should be a single, well-defined keyword that can be translated, e.g.: 'cyclestreet', 'network' or 'onto:cyclestreet', 'onto:network:red', ...
        ///
        /// It will be translated by the downstream user
        /// </summary>
        public IEnumerable<string> Amendments;

        /// <summary>
        /// The amended instruction
        /// </summary>
        public Instruction Instruction;

        public override string ToString()
        {
            return $"{Instruction} [{string.Join(",", Amendments)}]";
        }
    }


    public class Turn : Instruction

    {
        /// <summary>
        /// The amount of degrees to turn from the previous travel direction
        /// Negative is a right turn, positive is a left turn.
        /// Turns should be between -180 and 180
        /// </summary>
        public readonly int Degrees;

        /// <summary>
        /// The streetname onto which we are turning here
        /// Null if no streetname is given
        /// </summary>
        public readonly string Onto;

        public Turn(uint shape, int degrees, string turnOnto = null) : base(shape)
        {
            Degrees = degrees;
            Onto = turnOnto;
        }

        /**
         * Given a turn in degrees, this will normalize (thus make sure the angle is between -180° and 180°)
         * and cast to int
         */
        public Turn(uint shape, double degrees, string turnOnto = null) : base(shape)
        {
            if (degrees <= -180)
            {
                degrees += 360;
            }

            if (degrees > 180)
            {
                degrees -= 360;
            }

            Degrees = (int) degrees;
            Onto = turnOnto;
        }

        public override string ToString()
        {
            var onto = "";
            if (Onto != null)
            {
                onto = " onto " + Onto;
            }
            // Did you have enough 'onto' on the lines above?

            return $"Turn {Math.Abs(Degrees)} " + (Degrees < 0 ? "right" : "left" + onto);
        }
    }


    /// <summary>
    /// Follow along is the basic instruction, where a user just follows the road.
    /// This is used when there is no doubt about what to do (thus: no branches)
    ///
    /// Note that there might be an additional instruction embedded, such as a 'turn left', indicating that there is a bend in the road that should be followed
    /// 
    /// </summary>
    public class FollowAlong : Instruction
    {
        /// <summary>
        /// An extra 'subinstruction', e.g. if the road turns slightly right but there is no crossing, one should say
        /// 'folllow along the road which bends slightly right'
        /// </summary>
        public readonly Instruction Clarification;

        /// <summary>
        /// The name of the road to follow along
        /// Note that a name might not be known, in which case this will be null
        /// </summary>
        public readonly string Name;

        public FollowAlong(uint shape) : base(shape)
        {
        }

        public FollowAlong(uint shape, string name) : base(shape)
        {
            Name = name;
        }

        public FollowAlong(Instruction clarification, string name = null) : base(clarification.Shape)
        {
            Name = name;
            Clarification = clarification;
        }

        public override string ToString()
        {
            var road = "the road";
            if (Name != null)
            {
                road = Name;
            }

            return $"Follow {road} ({Clarification})";
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