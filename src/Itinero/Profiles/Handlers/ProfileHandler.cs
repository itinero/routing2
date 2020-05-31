namespace Itinero.Profiles.Handlers
{
    /// <summary>
    /// A profile handler.
    /// </summary>
    internal abstract class ProfileHandler
    {
        /// <summary>
        /// Moves this profile handler to the given edge.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        public abstract void MoveTo(RouterDbEdgeEnumerator enumerator);
        
        /// <summary>
        /// Gets the length (in cm) for the current edge.
        /// </summary>
        public abstract uint Length { get; }
        
        /// <summary>
        /// Gets the edge factor of the current edge.
        /// </summary>
        public abstract EdgeFactor EdgeFactor { get; }
        
        /// <summary>
        /// Gets the forward weight.
        /// </summary>
        public uint ForwardWeight => this.EdgeFactor.ForwardFactor * this.Length;
        
        /// <summary>
        /// Gets the backward weight.
        /// </summary>
        public uint BackwardWeight => this.EdgeFactor.BackwardFactor * this.Length;

        /// <summary>
        /// Returns true if the current edge can be accessed.
        /// </summary>
        public virtual bool CanAccess => this.EdgeFactor.BackwardFactor > 0 || this.EdgeFactor.ForwardFactor > 0;

        internal double GetForwardWeight(RouterDbEdgeEnumerator enumerator)
        {
            this.MoveTo(enumerator);
            return this.ForwardWeight;
        }

        internal double GetBackwardWeight(RouterDbEdgeEnumerator enumerator)
        {
            this.MoveTo(enumerator);
            return this.ForwardWeight;
        }
    }
}