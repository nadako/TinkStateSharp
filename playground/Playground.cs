using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TinkState;

class Playground
{
	static async Task Main()
	{
		var stateA = Observable.State("hello");
		var stateB = Observable.State("world");

		var o = Observable.Auto(async () =>
		{
			Console.WriteLine("computing");
			var a = stateA.Value;
			await Task.Delay(1000);
			var b = stateB.Value;
			return a + " " + b;
		});

		o.Bind(result => Console.WriteLine(result.Status switch
		{
			AsyncComputeStatus.Loading => "Loading...",
			AsyncComputeStatus.Done => "Done: " + result.Result,
			AsyncComputeStatus.Failed => "Failed: " + result.Exception,
		}));

		await Task.Delay(1500);

		stateB.Value = "Dan";

		Process.GetCurrentProcess().WaitForExit();
	}
}
