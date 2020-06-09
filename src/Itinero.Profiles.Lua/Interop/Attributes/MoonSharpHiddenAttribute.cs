﻿using System;

namespace Itinero.Profiles.Lua
{
	/// <summary>
	/// Forces a class member visibility to scripts. Can be used to hide public members. Equivalent to MoonSharpVisible(false).
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
		| AttributeTargets.Constructor | AttributeTargets.Event, Inherited = true, AllowMultiple = false)]
	internal sealed class MoonSharpHiddenAttribute : Attribute
	{
	}
}
