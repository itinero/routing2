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
        /// Gets the forward weight.
        /// </summary>
        public abstract uint ForwardWeight { get; }
        
        /// <summary>
        /// Gets the backward weight.
        /// </summary>
        public abstract uint BackwardWeight { get; }
        
        /// <summary>
        /// Gets the forward speed.
        /// </summary>
        public abstract uint ForwardSpeed { get; }
        
        /// <summary>
        /// Gets the backward speed.
        /// </summary>
        public abstract uint BackwardSpeed { get; }
        
        /// <summary>
        /// Gets the can stop flag.
        /// </summary>
        public abstract bool CanStop { get; }

        internal uint GetForwardWeight(RouterDbEdgeEnumerator enumerator)
        {
            this.MoveTo(enumerator);
            return this.ForwardWeight;
        }

        internal uint GetBackwardWeight(RouterDbEdgeEnumerator enumerator)
        {
            this.MoveTo(enumerator);
            return this.ForwardWeight;
        }
    }
}