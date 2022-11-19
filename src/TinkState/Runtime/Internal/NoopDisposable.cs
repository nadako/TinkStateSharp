using System;

namespace TinkState.Internal
{
	class NoopDisposable : IDisposable
	{
		public static readonly IDisposable Instance = new NoopDisposable();
		NoopDisposable() { }
		public void Dispose() { }
	}
}
