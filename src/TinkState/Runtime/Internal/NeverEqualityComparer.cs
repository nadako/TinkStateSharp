using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TinkState.Internal
{
	class NeverEqualityComparer<T> : IEqualityComparer<T>
	{
		public bool Equals(T x, T y)
		{
			return false;
		}

		[ExcludeFromCodeCoverage] // never used by us the the class is not public
		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
}