using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using TinkState.Model.Weaver;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Logger = TinkState.Model.Weaver.Logger;

namespace TinkState.Model
{
	public class UnityCompilationHook : ILPostProcessor
	{
		class UnityDebugLogger : Logger
		{
			public readonly List<DiagnosticMessage> Messages = new List<DiagnosticMessage>();

			public void Log(string message)
			{
				Messages.Add(new DiagnosticMessage
				{
					DiagnosticType = DiagnosticType.Warning,
					MessageData = message
				});
			}

			public void Error(string message)
			{
				Messages.Add(new DiagnosticMessage
				{
					DiagnosticType = DiagnosticType.Error,
					MessageData = message,
				});
			}
		}

		public override ILPostProcessor GetInstance() => this;

		public override bool WillProcess(ICompiledAssembly compiledAssembly)
		{
			return compiledAssembly.References.Any(path => Path.GetFileNameWithoutExtension(path) == "Nadako.TinkState.Model");
		}

		public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
		{
			var logger = new UnityDebugLogger();
			logger.Log("Processing " + compiledAssembly.Name);

			using (var stream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData))
			using (var symbols = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData))
			{
				var resolver = new DefaultAssemblyResolver();
				var dirs = compiledAssembly.References.Select(Path.GetDirectoryName).Distinct();
				foreach (var path in dirs)
				{
					resolver.AddSearchDirectory(path);
				}
				// logger.Log(string.Join(", ", resolver.GetSearchDirectories()));

				// return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.Messages);

				var readParams = new ReaderParameters
				{
					SymbolStream = symbols,
					ReadWrite = true,
					ReadSymbols = true,
					AssemblyResolver = resolver,
					ReflectionImporterProvider = new FixedReflectionImporterProvider()
				};
				var module = ModuleDefinition.ReadModule(stream, readParams);

				if (ModelWeaver.Weave(module, logger, out var modified))
				{
					if (modified)
					{
						var peOut = new MemoryStream();
						var pdbOut = new MemoryStream();
						var writeParameters = new WriterParameters
						{
							SymbolWriterProvider = new PortablePdbWriterProvider(),
							SymbolStream = pdbOut,
							WriteSymbols = true
						};
						module.Write(peOut, writeParameters);

						var newAsm = new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray());
						return new ILPostProcessResult(newAsm, logger.Messages);
					}
				}

				return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, logger.Messages);
			}
		}

		public class FixedReflectionImporterProvider : IReflectionImporterProvider
		{
			public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
			{
				return new FixedReflectionImporter(module);
			}

			public class FixedReflectionImporter : DefaultReflectionImporter
			{
				const string SystemPrivateCoreLib = "System.Private.CoreLib";

				readonly AssemblyNameReference fixedCoreLib;

				public FixedReflectionImporter(ModuleDefinition module) : base(module)
				{
					fixedCoreLib = module.AssemblyReferences.FirstOrDefault(a =>
						a.Name is "mscorlib" or "netstandard" or SystemPrivateCoreLib);
				}

				public override AssemblyNameReference ImportReference(AssemblyName name)
				{
					if (name.Name == SystemPrivateCoreLib && fixedCoreLib != null)
						return fixedCoreLib;

					return base.ImportReference(name);
				}
			}
		}
	}
}