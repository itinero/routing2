namespace Itinero.Profiles
{
    /// <summary>
    /// A factor returned by a vehicle profile to influence routing augmented with estimated speed. 
    /// </summary>
    public struct EdgeFactor
    {
        /// <summary>
        /// Creates a new edge factor.
        /// </summary>
        /// <param name="factorForward">The forward factor.</param>
        /// <param name="factorBackward">The backward factor.</param>
        /// <param name="speedForward">The forward speed.</param>
        /// <param name="speedBackward">The backward speed.</param>
        /// <param name="canStop">The can stop.</param>
        public EdgeFactor(uint factorForward, uint factorBackward,
            uint speedForward, uint speedBackward, bool canStop = true)
        {
            this.FactorForward = factorForward;
            this.FactorBackward = factorBackward;
            this.SpeedForward = speedForward;
            this.SpeedBackward = speedBackward;
            this.CanStop = canStop;
        }
        
        /// <summary>
        /// Gets the forward factor.
        /// </summary>
        public uint FactorForward { get; }
        
        /// <summary>
        /// Gets the backward factor.
        /// </summary>
        public uint FactorBackward { get; }
        
        /// <summary>
        /// Gets the forward speed.
        /// </summary>
        public uint SpeedForward { get; }
        
        /// <summary>
        /// Gets the backward speed.
        /// </summary>
        public uint SpeedBackward { get; }
        
        /// <summary>
        /// Gets the can stop flag.
        /// </summary>
        public bool CanStop { get; }

        /// <summary>
        /// Gets a static no-factor.
        /// </summary>
        public static EdgeFactor NoFactor => new EdgeFactor(0, 0, 0, 0);
    }
}