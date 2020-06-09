﻿using System;
using Itinero.Profiles.Lua.Interop.BasicDescriptors;

namespace Itinero.Profiles.Lua.Interop.StandardDescriptors.HardwiredDescriptors
{
	internal abstract class HardwiredUserDataDescriptor : DispatchingUserDataDescriptor
	{
		protected HardwiredUserDataDescriptor(Type T) :
			base(T, "::hardwired::" + T.Name)
		{

		}

	}
}
