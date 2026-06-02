namespace Itinero.Profiles;

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
    /// <param name="isLocalAccess">True if the edge is local-access only (e.g. <c>access=destination</c> for the profile's mode). Such edges may legitimately be used only when origin or destination is on or beyond them; the routing engine forbids them as through-traffic.</param>
    public EdgeFactor(uint forwardFactor, uint backwardFactor,
        ushort forwardSpeed, ushort backwardSpeed, bool canStop = true, bool isLocalAccess = false)
    {
        this.ForwardFactor = forwardFactor;
        this.BackwardFactor = backwardFactor;
        this.ForwardSpeed = forwardSpeed;
        this.BackwardSpeed = backwardSpeed;
        this.CanStop = canStop;
        this.IsLocalAccess = isLocalAccess;
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
    /// True iff the edge is local-access only for the profile's mode. See <see cref="EdgeFactor(uint, uint, ushort, ushort, bool, bool)"/>.
    /// </summary>
    public bool IsLocalAccess { get; }

    /// <summary>
    /// Gets a static no-factor.
    /// </summary>
    public static EdgeFactor NoFactor => new(0, 0, 0, 0);

    /// <summary>
    /// Gets the exact reverse, switches backward and forward.
    /// </summary>
    public EdgeFactor Reverse => new(this.BackwardFactor, this.ForwardFactor, this.BackwardSpeed, this.ForwardSpeed, this.CanStop, this.IsLocalAccess);

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

    public override int GetHashCode()
    {
        return (int)(this.ForwardFactor ^ (this.ForwardSpeed << 8) ^ (this.BackwardFactor << 16) ^ (this.ForwardSpeed << 24));
    }
}
