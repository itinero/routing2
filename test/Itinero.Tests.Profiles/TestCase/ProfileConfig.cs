namespace Itinero.Tests.Profiles.TestCase
{
    internal class ProfileConfig
    {
        /// <summary>
        /// Gets or sets the profile file name.
        /// </summary>
        public string File { get; set; }
        
        /// <summary>
        /// Gets the profile full name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the contraction flag.
        /// </summary>
        public bool Contract { get; set; }
    }
}