namespace Itinero.Profiles
{
    /// <summary>
    /// An 'EdgeFactor' contains the essential information to perform route planning, such as speed and priority in forward and backward direction.
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
            ForwardFactor = forwardFactor;
            BackwardFactor = backwardFactor;
            ForwardSpeed = forwardSpeed;
            BackwardSpeed = backwardSpeed;
            CanStop = canStop;
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
        public double BackwardSpeedMeterPerSecond => BackwardSpeed / 100.0;

        /// <summary>
        /// Gets the forward speed in ms/s multiplied by 100.
        /// </summary>
        public ushort ForwardSpeed { get; }

        /// <summary>
        /// Gets the backward speed in m/s.
        /// </summary>
        public double ForwardSpeedMeterPerSecond => ForwardSpeed / 100.0;

        /// <summary>
        /// Gets the can stop flag.
        /// </summary>
        public bool CanStop { get; }

        /// <summary>
        /// Returns true if empty.
        /// </summary>
        public bool IsNoFactor => this.ForwardFactor == 0 && this.BackwardFactor == 0;

        /// <summary>
        /// Gets a static no-factor.
        /// </summary>
        public static EdgeFactor NoFactor => new(0, 0, 0, 0);

        /// <summary>
        /// Gets the exact reverse, switches backward and forward.
        /// </summary>
        public EdgeFactor Reverse => new(BackwardFactor, ForwardFactor, BackwardSpeed, ForwardSpeed, CanStop);

        /// <inheritdoc/>
        public override string ToString()
        {
            var forwardSpeed = ForwardSpeed / 100.0 * 3.6;
            if (ForwardFactor == BackwardFactor &&
                ForwardSpeed == BackwardSpeed) {
                return $"{ForwardFactor:F1}({forwardSpeed:F1}km/h)";
            }

            var backwardSpeed = BackwardSpeed / 100.0 * 3.6;
            return $"F:{ForwardFactor:F1}({forwardSpeed:F1}km/h) B:{BackwardFactor:F1}({backwardSpeed:F1}km/h)";
        }

        // public override int GetHashCode()
        // {
        //     return (int) (this.ForwardFactor ^ this.ForwardSpeed << 8 ^ this.BackwardFactor << 16 ^ this.BackwardSpeed << 24);
        // }
        public override int GetHashCode()
        {
            return (73247435, this.ForwardFactor, this.ForwardSpeed, this.BackwardFactor, this.BackwardSpeed).GetHashCode();
        }
    }
}