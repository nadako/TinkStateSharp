using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace TinkState.Model
{
	public static class UnityCompilationHook
	{
		[InitializeOnLoadMethod]
		public static void OnInitializeOnLoad()
		{
			CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;

			if (!SessionState.GetBool("TINK_STATE_MODEL_WEAVED", false))
			{
				SessionState.SetBool("TINK_STATE_MODEL_WEAVED", true);
				SessionState.SetBool("TINK_STATE_MODEL_WEAVE_SUCCESS", true);
				WeaveExistingAssemblies();
			}
		}

		static void WeaveExistingAssemblies()
		{
			foreach (var assembly in CompilationPipeline.GetAssemblies())
			{
				if (File.Exists(assembly.outputPath))
				{
					OnCompilationFinished(assembly.outputPath, Array.Empty<CompilerMessage>());
				}
			}

			EditorUtility.RequestScriptReload();
		}

		static bool CompilerMessagesContainError(CompilerMessage[] messages) =>
			messages.Any(msg => msg.type == CompilerMessageType.Error);

		static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
		{
			if (CompilerMessagesContainError(messages))
			{
				Debug.Log("TinkState.Model: Did not weave because of compilation errors");
				return;
			}

			if (assemblyPath.Contains("-Editor") || assemblyPath.Contains(".Editor"))
			{
				return;
			}

			if (!WeaveFile(assemblyPath))
			{
				SessionState.SetBool("MIRROR_WEAVE_SUCCESS", false);
				Debug.LogError($"Weaving failed for {assemblyPath}");
			}
		}

		static bool WeaveFile(string assemblyPath)
		{
			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
			var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters {ReadWrite = true, ReadSymbols = true, AssemblyResolver = resolver});
			Debug.Log($"Weaving {assemblyPath}");
			var success = Weaver.Weave(module);
			if (success)
			{
				module.Write();
			}
			return success;
		}
	}
}