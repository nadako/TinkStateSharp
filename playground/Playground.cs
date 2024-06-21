using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TinkState;
using Timer = System.Timers.Timer;

class Playground
{
	static void Main()
	{
		var time = CreateTimeObservable();
		var o = Observable.Auto(() => (time.Value, time.Value.ToShortTimeString()));
		// var o = time;

		Console.WriteLine(o.Value);
		Thread.Sleep(2000);
		Console.WriteLine(o.Value);
		//
		// var counter = 0;
		// IDisposable binding = null;
		// binding = o.Bind(v =>
		// {
		// 	Console.WriteLine($"Current time is: {v}");
		// 	counter++;
		// 	if (counter >= 5) binding.Dispose();
		// });

		Process.GetCurrentProcess().WaitForExit();
	}

	private static Observable<DateTime> CreateTimeObservable()
	{
		var time = Observable.External(() => DateTime.UtcNow);

		var timer = new Timer(1000);

		var timerElapsedHandler = new ElapsedEventHandler((_, _) =>
		{
			Console.WriteLine("tick");
			time.Invalidate();
		});

		time.Subscribed += () =>
		{
			Console.WriteLine("wakeup");
			timer.Elapsed += timerElapsedHandler;
			timer.Start();
		};

		time.Unsubscribed += () =>
		{
			Console.WriteLine("sleep");
			timer.Elapsed -= timerElapsedHandler;
			timer.Stop();
		};

		return time;
	}
}
