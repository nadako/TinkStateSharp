namespace TinkState
{
	/// <summary>
	/// Piece of mutable observable data holding a value of type <typeparamref name="T"/> with an ability to set a new value.
	/// </summary>
	/// <typeparam name="T">Type of the value being managed by this observable.</typeparam>
	public interface State<T> : Observable<T>
	{
		/// <summary>
		/// Current value of this observable object.
		/// </summary>
		/// <remarks>
		/// Setting a different value to this property will trigger bindings and derived auto-observable updates.
		/// </remarks>
		/// <value>Current value of this observable object.</value>
		new T Value { get; set; }

		// TODO: better name
		void ForceInvalidate();
	}
}