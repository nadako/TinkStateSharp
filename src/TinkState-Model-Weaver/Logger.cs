namespace TinkState.Model.Weaver
{
	public interface Logger
	{
		void Debug(string message);
		void Error(string message, string file, int line, int column);
	}
}