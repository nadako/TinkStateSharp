using TinkState.Internal;

namespace TinkState
{
	/// <summary>
	/// Mechanism for scheduling binding invocations. Must be implemented for the target run-time to provide efficient batching.
	/// <seealso cref="Observable.Scheduler"/>
	/// </summary>
	public interface Scheduler
	{
		/// <summary>
		/// Simple scheduler that directly invokes given <see cref="Schedulable"/> objects.
		/// </summary>
		public static readonly Scheduler Direct = new DirectScheduler();

		/// <summary>
		/// Schedule given <see cref="Schedulable"/> (e.g. an observable binding) for invocation.
		/// </summary>
		/// <param name="schedulable">A schedulable object to invoke.</param>
		void Schedule(Schedulable schedulable);
	}

	/// <summary>
	/// Interface for objects schedulable for invocation by <see cref="Scheduler"/>.
	/// The most common implementors of this interface are observable bindings that needs to be scheduled to run in batches.
	/// </summary>
	public interface Schedulable
	{
		/// <summary>
		/// The logic that needs to be invoked on schedule.
		/// </summary>
		void Run();
	}
}