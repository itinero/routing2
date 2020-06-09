﻿
namespace Itinero.Profiles.Lua.Interop.BasicDescriptors
{
	/// <summary>
	/// Interface for descriptors with the capability of being serialized
	/// for later hardwiring.
	/// </summary>
	internal interface IWireableDescriptor 
	{
		/// <summary>
		/// Prepares the descriptor for hard-wiring.
		/// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
		/// </summary>
		/// <param name="t">The table to be filled</param>
		void PrepareForWiring(Table t);
	}
}
