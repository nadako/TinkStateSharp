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
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			if (!SessionState.GetBool("TINK_STATE_MODEL_WEAVED", false))
			{
				SessionState.SetBool("TINK_STATE_MODEL_WEAVED", true);
				WeaveExistingAssemblies();
			}
		}

		static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				if (!SessionState.GetBool("TINK_STATE_MODEL_WEAVE_SUCCESS", false))
				{
					WeaveExistingAssemblies();

					if (!SessionState.GetBool("TINK_STATE_MODEL_WEAVE_SUCCESS", false))
					{
						Debug.LogError("Can't enter play mode until weaver issues are resolved.");
						EditorApplication.isPlaying = false;
					}
				}
			}
		}

		static void WeaveExistingAssemblies()
		{
			SessionState.SetBool("TINK_STATE_MODEL_WEAVE_SUCCESS", true);

			foreach (var assembly in CompilationPipeline.GetAssemblies())
			{
				if (File.Exists(assembly.outputPath))
				{
					OnCompilationFinished(assembly.outputPath, Array.Empty<CompilerMessage>());
				}
			}

			EditorUtility.RequestScriptReload();
		}

		static void OnCompilationFinished(string assemblyPath, CompilerMessage[] messages)
		{
			if (messages.Any(msg => msg.type == CompilerMessageType.Error))
			{
				Debug.Log("TinkState.Model: Did not weave because of compilation errors");
				return;
			}

			if (assemblyPath.Contains("-Editor") || assemblyPath.Contains(".Editor"))
			{
				return;
			}

			var tinkStateModelAssembly = CompilationPipeline.GetAssemblies().FirstOrDefault(assembly => assembly.name == "Nadako.TinkState.Model");
			if (tinkStateModelAssembly == null)
			{
				Debug.LogError("Failed to find TinkState# Model runtime assembly");
				return;
			}

			var tinkStateModelDll = tinkStateModelAssembly.outputPath;
			if (!File.Exists(tinkStateModelDll))
			{
				// model assembly is not yet build, meaning that the current assembly doesn't need weaving since it doesn't depend on model,
				// otherwise it would be processed after model assembly
				return;
			}

			if (!WeaveFile(assemblyPath))
			{
				SessionState.SetBool("TINK_STATE_MODEL_WEAVE_SUCCESS", false);
				Debug.LogError($"Weaving failed for {assemblyPath}");
			}
		}

		static bool WeaveFile(string assemblyPath)
		{
			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
			var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters {ReadWrite = true, ReadSymbols = true, AssemblyResolver = resolver});
			if (Weaver.Weave(module, out var modified))
			{
				if (modified)
				{
					module.Write(new WriterParameters {WriteSymbols = true});
				}

				return true;
			}
			return false;
		}
	}
}