namespace Itinero.Profiles
{
    /// <summary>
    /// A turn cost factor used to customize turn cost handling per profile.
    /// </summary>
    /// <remarks>
    /// There are few special values:
    /// - 0: the turn costs are ignored.
    /// - uint.maxvalue: the turn costs are binary, like in turn restrictions.
    /// </remarks>
    public readonly struct TurnCostFactor
    {
        /// <summary>
        /// Creates a new turn cost factor.
        /// </summary>
        /// <param name="factor">The factor.</param>
        public TurnCostFactor(uint factor = 0)
        {
            this.Factor = factor;
        }
        
        /// <summary>
        /// Gets the turn cost factor. 
        /// </summary>
        public uint Factor { get; }

        /// <summary>
        /// Gets the value to multiply the turn cost with to calculate the weight.
        /// </summary>
        public double CostFactor => (this.Factor / 10.0);

        /// <summary>
        /// Returns true if this factor is empty, causing the turn costs to be ignored.
        /// </summary>
        public bool IsEmpty => this.Factor == 0;

        /// <summary>
        /// Returns true if the factor is binary, for example in the case of a turn-restriction cost.
        /// </summary>
        public bool IsBinary => this.Factor == uint.MaxValue;
        
        /// <summary>
        /// Gets the default empty turn cost factor.
        /// </summary>
        public static TurnCostFactor Empty = new TurnCostFactor(0);
        
        /// <summary>
        /// Gets the binary turn cost factor.
        /// </summary>
        public static TurnCostFactor Binary = new TurnCostFactor(uint.MaxValue);
    }
}