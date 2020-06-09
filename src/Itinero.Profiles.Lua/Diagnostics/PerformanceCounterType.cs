﻿
namespace Itinero.Profiles.Lua.Diagnostics
{
	/// <summary>
	/// Enumeration of unit of measures of the performance counters
	/// </summary>
	internal enum PerformanceCounterType
	{
		/// <summary>
		/// The performance counter is specified in bytes (of memory)
		/// </summary>
		MemoryBytes,
		/// <summary>
		/// The performance counter is specified in milliseconds
		/// </summary>
		TimeMilliseconds,
	}
}
