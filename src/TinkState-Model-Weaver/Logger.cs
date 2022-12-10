namespace TinkState.Model.Weaver
{
	public interface Logger
	{
		void Log(string message);
		void Error(string message); // TODO: report file+line+column for unity IL processor and msbuild tasks
	}
}