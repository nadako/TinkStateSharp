using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using TinkState;

class Playground
{
	static void Main()
	{
		var timer = new Timer(1000);
		ElapsedEventHandler timerElapsedHandler = null;

		var time = Observable.External(
			() => DateTime.UtcNow,
			invalidate =>
			{
				Console.WriteLine("wakeup");
				timerElapsedHandler = (_, _) =>
				{
					Console.WriteLine("tick");
					invalidate();
				};
				timer.Elapsed += timerElapsedHandler;
				timer.Start();
			},
			() =>
			{
				Console.WriteLine("sleep");
				timer.Elapsed -= timerElapsedHandler;
				timer.Stop();
			}
		);

		var counter = 0;
		IDisposable binding = null;
		binding = time.Bind(v =>
		{
			Console.WriteLine($"Current time is: {v}");
			counter++;
			if (counter >= 5) binding.Dispose();
		});

		Process.GetCurrentProcess().WaitForExit();
	}
}
