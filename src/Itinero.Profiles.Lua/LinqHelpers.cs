﻿using System.Collections.Generic;
using System.Linq;

namespace Itinero.Profiles.Lua
{
	/// <summary>
	/// LINQ helper methods
	/// </summary>
	internal static class LinqHelpers
	{
		/// <summary>
		/// Converts the specified enumerable dynvalues of a given script type to objects of a given type
		/// </summary>
		/// <typeparam name="T">The desired type</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		/// <param name="type">The type.</param>
		internal static IEnumerable<T> Convert<T>(this IEnumerable<DynValue> enumerable, DataType type)
		{
			return enumerable.Where(v => v.Type == type).Select(v => v.ToObject<T>());
		}

		/// <summary>
		/// Filters an enumeration for items of the given script type
		/// </summary>
		/// <param name="enumerable">The enumerable.</param>
		/// <param name="type">The script type.</param>
		internal static IEnumerable<DynValue> OfDataType(this IEnumerable<DynValue> enumerable, DataType type)
		{
			return enumerable.Where(v => v.Type == type);
		}

		/// <summary>
		/// Converts the elements to CLR objects
		/// </summary>
		/// <param name="enumerable">The enumerable.</param>
		internal static IEnumerable<object> AsObjects(this IEnumerable<DynValue> enumerable)
		{
			return enumerable.Select(v => v.ToObject());
		}

		/// <summary>
		/// Converts the elements to CLR objects of the desired type
		/// </summary>
		/// <typeparam name="T">The desired type</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		internal static IEnumerable<T> AsObjects<T>(this IEnumerable<DynValue> enumerable)
		{
			return enumerable.Select(v => v.ToObject<T>());
		}
	}
}
