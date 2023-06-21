namespace TinkState
{
	public interface ManualObservableSource<T>
	{
		Observable<T> Observe();
		void Invalidate();
		void Update(T newValue);
	}
}