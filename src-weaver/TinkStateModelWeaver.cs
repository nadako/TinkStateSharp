using System;
using Mono.Cecil;
using System.IO;
using TinkState.Model.Weaver;
using Logger = TinkState.Model.Weaver.Logger;

namespace TinkState.Model
{
	public class TinkStateModelWeaver
	{
		class ConsoleLogger : Logger
		{
			public void Debug(string message)
			{
				Console.WriteLine(message);
			}

			public void Error(string message, string file, int line, int column)
			{
				Console.WriteLine("ERROR: " + message);
			}
		}

		static void Main(string[] args)
		{
			var assemblyPath = args[0];

			var logger = new ConsoleLogger();
			logger.Debug("Processing " + assemblyPath);

			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

			var readParams = new ReaderParameters
			{
				ReadWrite = true,
				ReadSymbols = true,
				AssemblyResolver = resolver,
			};
			var module = ModuleDefinition.ReadModule(assemblyPath, readParams);

			if (ModelWeaver.Weave(module, logger, out var modified))
			{
				if (modified)
				{
					var writeParameters = new WriterParameters
					{
						WriteSymbols = true
					};
					module.Write(writeParameters);
				}
			}
		}
	}
}