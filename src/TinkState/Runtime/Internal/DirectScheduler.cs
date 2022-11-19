namespace TinkState.Internal
{
	class DirectScheduler : Scheduler
	{
		public void Schedule(Schedulable schedulable)
		{
			schedulable.Run();
		}
	}
}