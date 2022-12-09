namespace TinkState.Model
{
	public interface Model { }

	public static class ModelExtensions
	{
		public static Observable<T> GetObservable<T>(this Model model, string field)
		{
			return null;
		}
	}
}