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
        public EdgeFactor(uint factorForward, uint factorBackward,
            uint speedForward, uint speedBackward)
        {
            this.FactorForward = factorForward;
            this.FactorBackward = factorBackward;
            this.SpeedForward = speedForward;
            this.SpeedBackward = speedBackward;
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
    }
}