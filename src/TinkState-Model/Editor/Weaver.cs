using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnityEngine;

namespace TinkState.Model
{
	class Weaver
	{
		public static bool Weave(ModuleDefinition module)
		{
			var tinkStateRef = module.AssemblyReferences.First(r => r.Name == "Nadako.TinkState");
			var tinkStateAssembly = module.AssemblyResolver.Resolve(tinkStateRef);

			var tinkStateModelRef = module.AssemblyReferences.First(r => r.Name == "Nadako.TinkState.Model");
			var tinkStateModelAssembly = module.AssemblyResolver.Resolve(tinkStateModelRef);
			var modelInternalType = tinkStateModelAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Model.ModelInternal");

			var observableType = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Observable`1");
			var stateType = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.State`1");
			var observableClass = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Observable");
			const string ObservableAttributeName = "TinkState.Model.ObservableAttribute";
			const string CompilerGeneratedAttributeName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

			foreach (var type in module.Types)
			{
				if (!type.IsClass) continue;
				if (!type.HasInterfaces ||
				    !type.Interfaces.Any(i => i.InterfaceType.FullName == "TinkState.Model.Model")) continue;

				Debug.Log("Weaving " + type.FullName);
				var inits = new List<Instruction[]>();
				var obsfields = new List<(string name, FieldReference field)>();
				foreach (var prop in type.Properties)
				{
					if (!prop.HasCustomAttribute(ObservableAttributeName)) continue;

					if (prop.GetMethod == null) throw new Exception("Observable properties must have a get method");

					if (prop.SetMethod == null) // auto-observable
					{
						if (prop.GetMethod.HasCustomAttribute(CompilerGeneratedAttributeName)) throw new Exception("Read-only Observable properties must have a non-automatic get method");

						var getMethod = prop.GetMethod;

						var hoistedGetMethod = new MethodDefinition("<" + prop.Name + ">Tink_Getter",
							MethodAttributes.Private, getMethod.ReturnType);
						hoistedGetMethod.Body = new MethodBody(hoistedGetMethod);
						foreach (var i in getMethod.Body.Instructions)
							hoistedGetMethod.Body.Instructions.Add(i);
						type.Methods.Add(hoistedGetMethod);


						var backingFieldType = module.ImportReference(observableType).MakeGenericInstanceType(prop.PropertyType);
						var backingField =
							new FieldDefinition($"<{prop.Name}>k__BackingField", FieldAttributes.Private, backingFieldType);
						type.Fields.Add(backingField);

						var observableGetValue = observableType.Methods.First(m => m.Name == "get_Value");
						var getValue = new MethodReference("get_Value", observableGetValue.ReturnType, backingFieldType)
							{HasThis = true};

						var il = getMethod.Body.GetILProcessor();
						il.Clear();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, backingField);
						il.Emit(OpCodes.Callvirt, getValue);
						il.Emit(OpCodes.Ret);

						var autoCtorMethod = module.ImportReference(observableClass.Methods.First(m => m.Name == "Auto"));
						var autoCtorMethodInstance = new GenericInstanceMethod(autoCtorMethod);
						autoCtorMethodInstance.GenericArguments.Add(prop.PropertyType);


						var funcType = module.ImportReference(typeof(Func<>)).MakeGenericInstanceType(prop.PropertyType);
						var funcCtor = module.ImportReference(funcType.Resolve().GetConstructors().First());

						inits.Add(new[]
						{
							Instruction.Create(OpCodes.Ldarg_0),

							Instruction.Create(OpCodes.Ldarg_0),
							Instruction.Create(OpCodes.Ldftn, hoistedGetMethod), // TODO: add caching for non-closures like roslyn does?
							Instruction.Create(OpCodes.Newobj, funcCtor.MakeHostInstanceGeneric(module, funcType)),

							Instruction.Create(OpCodes.Ldnull), // comparer

							Instruction.Create(OpCodes.Call, autoCtorMethodInstance),

							Instruction.Create(OpCodes.Stfld, backingField),
						});
						obsfields.Add((prop.Name, backingField));
					}
					else // state
					{
						if (!prop.GetMethod.HasCustomAttribute(CompilerGeneratedAttributeName)) throw new Exception("Writable observable properties must have an automatic get method");
						if (!prop.SetMethod.HasCustomAttribute(CompilerGeneratedAttributeName)) throw new Exception("Writable observable properties must have an automatic set method");

						var originalBackingField = prop.GetBackingField();
						type.Fields.Remove(originalBackingField);

						var backingFieldType = module.ImportReference(stateType).MakeGenericInstanceType(prop.PropertyType);
						var backingField =
							new FieldDefinition(originalBackingField.Name, FieldAttributes.Private, backingFieldType);
						type.Fields.Add(backingField);

						var stateGetValue = stateType.Methods.First(m => m.Name == "get_Value");
						var getValue = new MethodReference("get_Value", stateGetValue.ReturnType, backingFieldType)
							{HasThis = true};

						var il = prop.GetMethod.Body.GetILProcessor();
						il.Clear();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, backingField);
						il.Emit(OpCodes.Callvirt, getValue);
						il.Emit(OpCodes.Ret);

						var stateSetValue = stateType.Methods.First(m => m.Name == "set_Value");
						var setValue = new MethodReference("set_Value", stateSetValue.ReturnType, backingFieldType)
							{HasThis = true};
						setValue.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, stateSetValue.Parameters[0].ParameterType));

						il = prop.SetMethod.Body.GetILProcessor();
						il.Clear();
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, backingField);
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Callvirt, setValue);
						il.Emit(OpCodes.Ret);

						var stateCtorMethod = module.ImportReference(observableClass.Methods.First(m => m.Name == "State"));
						var stateCtorMethodInstance = new GenericInstanceMethod(stateCtorMethod);
						stateCtorMethodInstance.GenericArguments.Add(prop.PropertyType);

						// TODO: find initial value in ctor (assignment to the original backing field) and push it instead of default
						inits.Add(new[]
						{
							Instruction.Create(OpCodes.Ldarg_0),
							Instruction.Create(OpCodes.Ldnull), // initial value (TODO: default)
							Instruction.Create(OpCodes.Ldnull), // comparer
							Instruction.Create(OpCodes.Call, stateCtorMethodInstance),
							Instruction.Create(OpCodes.Stfld, backingField),
						});
						obsfields.Add((prop.Name, backingField));
					}

					// Debug.Log(" - " + prop.FullName);
				}

				if (inits.Count > 0)
				{
					// TODO: hoist that into a method if there's more than 1 ctor?
					var hasCtor = false;
					foreach (var ctor in type.GetConstructors())
					{
						hasCtor = true;
						for (int i = inits.Count - 1; i >= 0; i--)
						{
							var init = inits[i];
							for (int j = init.Length - 1; j >= 0; j--)
							{
								ctor.Body.Instructions.Insert(0, init[j]);
							}
						}
					}

					if (!hasCtor) throw new Exception("No constructor O_o");
				}

				type.Interfaces.Add(new InterfaceImplementation(module.ImportReference(modelInternalType)));

				var interfaceGetObservableMethod = module.ImportReference(modelInternalType.Methods.First(m => m.Name == "GetObservable"));

				var returnType = module.ImportReference(observableType);
				var getObservableMethod = new MethodDefinition("TinkState.Model.ModelInternal.GetObservable",
					MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual, returnType);
				var p = new GenericParameter("T", getObservableMethod);
				getObservableMethod.GenericParameters.Add(p);
				getObservableMethod.MethodReturnType.ReturnType =
					module.ImportReference(observableType).MakeGenericInstanceType(p);
				getObservableMethod.Body = new MethodBody(getObservableMethod);
				getObservableMethod.Body.InitLocals = true;
				{
					var stringEq = module.ImportReference(typeof(string).GetMethod("op_Equality"));

					var il = getObservableMethod.Body.GetILProcessor();
					foreach (var (fieldName, fieldRef) in obsfields)
					{
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Ldstr, fieldName);
						il.Emit(OpCodes.Call, stringEq);
						var elseEntryPoint = Instruction.Create(OpCodes.Nop);
						il.Emit(OpCodes.Brfalse, elseEntryPoint);
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldfld, fieldRef);
						il.Emit(OpCodes.Ret);
						il.Append(elseEntryPoint);
					}
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ret);
				}
				getObservableMethod.Overrides.Add(interfaceGetObservableMethod);
				getObservableMethod.Parameters.Add(new ParameterDefinition("field", ParameterAttributes.None, module.TypeSystem.String));

				type.Methods.Add(getObservableMethod);
			}

			return true;
		}
	}

	static class Ext
	{
		public static FieldDefinition GetBackingField(this PropertyDefinition prop)
		{
			foreach (var instruction in prop.GetMethod.Body.Instructions)
			{
				if (instruction.OpCode == OpCodes.Ldfld) return ((FieldReference) instruction.Operand).Resolve();
			}
			throw new Exception("Couldn't find backing field load instruction in the get method");
		}

		public static bool HasCustomAttribute(this ICustomAttributeProvider provider, string attributeName)
		{
			return provider.HasCustomAttributes &&
			       provider.CustomAttributes.Any(a => a.AttributeType.FullName == attributeName);
		}

		public static MethodReference MakeHostInstanceGeneric(this MethodReference self, ModuleDefinition module, GenericInstanceType instanceType)
		{
			MethodReference reference = new MethodReference(self.Name, self.ReturnType, instanceType)
			{
				CallingConvention = self.CallingConvention,
				HasThis = self.HasThis,
				ExplicitThis = self.ExplicitThis
			};

			foreach (ParameterDefinition parameter in self.Parameters)
				reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

			foreach (GenericParameter generic_parameter in self.GenericParameters)
				reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

			return module.ImportReference(reference);
		}

	}
}
