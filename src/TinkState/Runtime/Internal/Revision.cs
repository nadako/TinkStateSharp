namespace TinkState.Internal
{
	static class Revision
	{
		static long counter;

		public static long New()
		{
			counter++;
			return counter;
		}
	}
}