namespace Itinero.Profiles
{
    /// <summary>
    /// A factor returned by a vehicle profile to influence routing augmented with estimated speed. 
    /// </summary>
    public readonly struct EdgeFactor
    {
        /// <summary>
        /// Creates a new edge factor.
        /// </summary>
        /// <param name="forwardFactor">The forward factor.</param>
        /// <param name="backwardFactor">The backward factor.</param>
        /// <param name="forwardSpeed">The forward speed in ms/s multiplied by 100.</param>
        /// <param name="backwardSpeed">The backward speed in ms/s multiplied by 100.</param>
        /// <param name="canStop">The can stop.</param>
        public EdgeFactor(uint forwardFactor, uint backwardFactor,
            ushort forwardSpeed, ushort backwardSpeed, bool canStop = true)
        {
            this.ForwardFactor = forwardFactor;
            this.BackwardFactor = backwardFactor;
            this.ForwardSpeed = forwardSpeed;
            this.BackwardSpeed = backwardSpeed;
            this.CanStop = canStop;
        }
        
        /// <summary>
        /// Gets the forward factor, multiplied by an edge distance this is the weight.
        /// </summary>
        public uint ForwardFactor { get; }
        
        /// <summary>
        /// Gets the backward factor, multiplied by an edge distance this is the weight.
        /// </summary>
        public uint BackwardFactor { get; }
        
        /// <summary>
        /// Gets the backward speed in m/s multiplied by 100.
        /// </summary>
        public ushort BackwardSpeed { get; }

        /// <summary>
        /// Gets the backward speed in m/s.
        /// </summary>
        public double BackwardSpeedMeterPerSecond => this.BackwardSpeed / 100.0;

        /// <summary>
        /// Gets the forward speed in ms/s multiplied by 100.
        /// </summary>
        public ushort ForwardSpeed { get; }

        /// <summary>
        /// Gets the backward speed in m/s.
        /// </summary>
        public double ForwardSpeedMeterPerSecond => this.ForwardSpeed / 100.0;
        
        /// <summary>
        /// Gets the can stop flag.
        /// </summary>
        public bool CanStop { get; }

        /// <summary>
        /// Gets a static no-factor.
        /// </summary>
        public static EdgeFactor NoFactor => new EdgeFactor(0, 0, 0, 0);
        
        /// <summary>
        /// Gets the exact reverse, switches backward and forward.
        /// </summary>
        public EdgeFactor Reverse => new EdgeFactor(this.BackwardFactor, this.ForwardFactor, this.BackwardSpeed, this.ForwardSpeed, this.CanStop);

        /// <inheritdoc/>
        public override string ToString()
        {
            var forwardSpeed = this.ForwardSpeed / 100.0 * 3.6;
            if (this.ForwardFactor == this.BackwardFactor &&
                this.ForwardSpeed == this.BackwardSpeed)
            {
                return $"{this.ForwardFactor:F1}({forwardSpeed:F1}km/h)";
            }
            var backwardSpeed = this.BackwardSpeed / 100.0 * 3.6;
            return $"F:{this.ForwardFactor:F1}({forwardSpeed:F1}km/h) B:{this.BackwardFactor:F1}({backwardSpeed:F1}km/h)";
        }
    }
}