namespace TinkState.Internal
{
	interface ValueProvider<out T>
	{
		T GetCurrentValue();
	}
}
