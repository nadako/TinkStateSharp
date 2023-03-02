using Godot;
using System.Collections.Generic;

namespace TinkState
{
	public sealed class GodotBatchScheduler : Scheduler
	{
		List<Schedulable> queue = new List<Schedulable>();
		bool scheduled;

		public void Process()
		{
			if (scheduled)
			{
				Progress(100);
			}
		}

		public void Schedule(Schedulable schedulable)
		{
			queue.Add(schedulable);
			scheduled = true;
		}

		void Progress(ulong maxMsec)
		{
			var end = GetTimeStamp() + maxMsec;
			do
			{
				var old = queue;
				queue = new List<Schedulable>();
				foreach (var o in old) o.Run();
			} while (queue.Count > 0 && GetTimeStamp() < end);

			if (queue.Count == 0)
			{
				scheduled = false;
			}
		}

		static ulong GetTimeStamp()
		{
			return Time.GetTicksMsec();
		}
	}
}