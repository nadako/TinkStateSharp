using Godot;
using TinkState;

public partial class TinkStateBatchScheduler : Node
{
	public override void _Ready()
	{
		var scheduler = new GodotBatchScheduler();
		Observable.Scheduler = scheduler;
		GetTree().ProcessFrame += scheduler.Process;
	}
}
