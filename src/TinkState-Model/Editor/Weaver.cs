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
		public static bool Weave(ModuleDefinition module, out bool modified)
		{
			modified = false;

			if (!IsUsingModels(module)) return true;

			Weaver weaver = null;
			foreach (var type in module.Types)
			{
				if (IsModelClass(type))
				{
					weaver ??= new Weaver(module);
					weaver.WeaveModelClass(type);
					modified = true;
				}
			}

			return true;
		}

		static bool IsUsingModels(ModuleDefinition module)
		{
			return module.HasAssemblyReferences && module.AssemblyReferences.Any(r => r.Name == "Nadako.TinkState.Model");
		}

		static bool IsModelClass(TypeDefinition type)
		{
			return type.IsClass && type.HasInterfaces && type.Interfaces.Any(i => i.InterfaceType.FullName == "TinkState.Model.Model");
		}

		const string ObservableAttributeName = "TinkState.Model.ObservableAttribute";
		const string CompilerGeneratedAttributeName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

		ModuleDefinition module;
		TypeDefinition observableType;
		TypeDefinition stateType;
		TypeDefinition modelInternalType;
		MethodReference modelInternalGetObservableMethod;
		MethodReference stateCtorMethod;
		MethodReference autoCtorMethod;
		MethodReference observableGetValueMethod;
		MethodReference stateGetValueMethod;
		MethodReference stateSetValueMethod;
		System.Reflection.MethodBase stringEqualsMethod;
		Type funcType;

		Weaver(ModuleDefinition module)
		{
			this.module = module;

			var tinkStateRef = module.AssemblyReferences.First(r => r.Name == "Nadako.TinkState");
			var tinkStateAssembly = module.AssemblyResolver.Resolve(tinkStateRef);
			observableType = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Observable`1");
			observableGetValueMethod = observableType.Methods.First(m => m.Name == "get_Value");
			stateType = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.State`1");
			stateGetValueMethod = stateType.Methods.First(m => m.Name == "get_Value");
			stateSetValueMethod = stateType.Methods.First(m => m.Name == "set_Value");

			var observableClass = tinkStateAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Observable");
			stateCtorMethod = observableClass.Methods.First(m => m.Name == "State");
			autoCtorMethod = observableClass.Methods.First(m => m.Name == "Auto");

			var modelAssemblyRef = module.AssemblyReferences.First(r => r.Name == "Nadako.TinkState.Model");
			var modelAssembly = module.AssemblyResolver.Resolve(modelAssemblyRef);
			modelInternalType = modelAssembly.MainModule.Types.First(t => t.FullName == "TinkState.Model.ModelInternal");
			modelInternalGetObservableMethod = modelInternalType.Methods.First(m => m.Name == "GetObservable");

			stringEqualsMethod = typeof(string).GetMethod("op_Equality");
			funcType = typeof(Func<>);
		}

		struct FieldData
		{
			public string PropertyName;
			public FieldReference BackingField;
			public Instruction[] InitCode;
		}

		void WeaveModelClass(TypeDefinition type)
		{
			Debug.Log("Weaving " + type.FullName);

			var backingFields = new List<FieldData>();

			foreach (var prop in type.Properties)
			{
				if (!prop.HasCustomAttribute(ObservableAttributeName)) continue;

				if (prop.GetMethod == null) throw new Exception("Observable properties must have a get method");

				if (prop.SetMethod == null)
				{
					CreateAutoObservable(type, prop, backingFields);
				}
				else
				{
					CreateState(type, prop, backingFields);
				}
			}

			AddBackingFieldInits(type, backingFields);
			ImplementModelInternal(type, backingFields);
		}

		void CreateAutoObservable(TypeDefinition type, PropertyDefinition prop, List<FieldData> backingFields)
		{
			if (prop.GetMethod.HasCustomAttribute(CompilerGeneratedAttributeName))
			{
				// auto-observables require actual computation code
				throw new Exception("Read-only Observable properties must have a non-automatic get method");
			}

			var getMethod = prop.GetMethod;

			// move get logic into a separate method since it'll be used as a computation for the auto-observable
			var hoistedGetMethod = new MethodDefinition("<" + prop.Name + ">TinkStateModel_Compute",
				MethodAttributes.Private, getMethod.ReturnType); // TODO: check attributes
			hoistedGetMethod.Body = new MethodBody(hoistedGetMethod);
			foreach (var i in getMethod.Body.Instructions)
				hoistedGetMethod.Body.Instructions.Add(i);
			type.Methods.Add(hoistedGetMethod);

			// add a backing field for the auto-observable
			var backingFieldType = module.ImportReference(observableType).MakeGenericInstanceType(prop.PropertyType);
			var backingField = new FieldDefinition($"<{prop.Name}>k__BackingField", FieldAttributes.Private, backingFieldType);
			type.Fields.Add(backingField);

			var getValue = new MethodReference(observableGetValueMethod.Name, observableGetValueMethod.ReturnType, backingFieldType) {HasThis = true};

			var il = getMethod.Body.GetILProcessor();
			il.Clear();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, backingField);
			il.Emit(OpCodes.Callvirt, getValue);
			il.Emit(OpCodes.Ret);

			var autoCtorMethodInstance = new GenericInstanceMethod(module.ImportReference(autoCtorMethod));
			autoCtorMethodInstance.GenericArguments.Add(prop.PropertyType);

			var appliedFuncType = module.ImportReference(funcType).MakeGenericInstanceType(prop.PropertyType);
			var funcCtor = module.ImportReference(appliedFuncType.Resolve().GetConstructors().First());

			backingFields.Add(new FieldData
			{
				PropertyName = prop.Name,
				BackingField = backingField,
				InitCode = new[]
				{
					Instruction.Create(OpCodes.Ldarg_0),

					Instruction.Create(OpCodes.Ldftn, hoistedGetMethod), // TODO: add caching for non-closures like roslyn does?
					Instruction.Create(OpCodes.Newobj, funcCtor.MakeHostInstanceGeneric(module, appliedFuncType)),

					Instruction.Create(OpCodes.Ldnull), // comparer

					Instruction.Create(OpCodes.Call, autoCtorMethodInstance),
				}
			});
		}

		void CreateState(TypeDefinition type, PropertyDefinition prop, List<FieldData> backingFields)
		{
			if (!prop.GetMethod.CheckAndRemoveCustomAttribute(CompilerGeneratedAttributeName))
				throw new Exception("Observable state properties must have an automatic get method");
			if (!prop.SetMethod.CheckAndRemoveCustomAttribute(CompilerGeneratedAttributeName))
				throw new Exception("Observable state properties must have an automatic set method");

			var originalBackingField = prop.GetBackingField();
			type.Fields.Remove(originalBackingField);

			var backingFieldType = module.ImportReference(stateType).MakeGenericInstanceType(prop.PropertyType);
			var backingField =
				new FieldDefinition(originalBackingField.Name, FieldAttributes.Private, backingFieldType);
			type.Fields.Add(backingField);

			var getValue = new MethodReference(stateGetValueMethod.Name, stateGetValueMethod.ReturnType, backingFieldType) {HasThis = true};

			var il = prop.GetMethod.Body.GetILProcessor();
			il.Clear();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, backingField);
			il.Emit(OpCodes.Callvirt, getValue);
			il.Emit(OpCodes.Ret);

			var setValue = new MethodReference(stateSetValueMethod.Name, stateSetValueMethod.ReturnType, backingFieldType) {HasThis = true};
			setValue.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, stateSetValueMethod.Parameters[0].ParameterType));

			il = prop.SetMethod.Body.GetILProcessor();
			il.Clear();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, backingField);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, setValue);
			il.Emit(OpCodes.Ret);

			var stateCtorMethodInstance = new GenericInstanceMethod(module.ImportReference(stateCtorMethod));
			stateCtorMethodInstance.GenericArguments.Add(prop.PropertyType);

			backingFields.Add(new FieldData
			{
				PropertyName = prop.Name,
				BackingField = backingField,
				InitCode = new[]
				{
					// TODO: find initial value in ctor (assignment to the original backing field) and push it instead of default
					Instruction.Create(OpCodes.Ldnull), // initial value (TODO: default)
					Instruction.Create(OpCodes.Ldnull), // comparer
					Instruction.Create(OpCodes.Call, stateCtorMethodInstance),
				}
			});
		}

		void ImplementModelInternal(TypeDefinition type, List<FieldData> fields)
		{
			// add interface implementation record
			type.Interfaces.Add(new InterfaceImplementation(module.ImportReference(modelInternalType)));

			// create GetObservable<T> method
			var getObservableMethod = new MethodDefinition("TinkState.Model.ModelInternal.GetObservable",
				MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig |
				MethodAttributes.Virtual, // TODO: check attributes
				module.TypeSystem.Void);

			var typeParam = new GenericParameter("T", getObservableMethod);
			getObservableMethod.GenericParameters.Add(typeParam);

			getObservableMethod.MethodReturnType.ReturnType = module.ImportReference(observableType).MakeGenericInstanceType(typeParam);

			getObservableMethod.Body = new MethodBody(getObservableMethod);
			var il = getObservableMethod.Body.GetILProcessor();
			foreach (var field in fields)
			{
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldstr, field.PropertyName);
				il.Emit(OpCodes.Call, module.ImportReference(stringEqualsMethod));
				var elseLabel = Instruction.Create(OpCodes.Nop);
				il.Emit(OpCodes.Brfalse, elseLabel);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, field.BackingField);
				il.Emit(OpCodes.Ret);
				il.Append(elseLabel);
			}

			il.Emit(OpCodes.Ldnull); // TODO: throw instead
			il.Emit(OpCodes.Ret);

			getObservableMethod.Overrides.Add(module.ImportReference(modelInternalGetObservableMethod));
			getObservableMethod.Parameters.Add(new ParameterDefinition("field", ParameterAttributes.None, module.TypeSystem.String));

			type.Methods.Add(getObservableMethod);
		}

		void AddBackingFieldInits(TypeDefinition type, List<FieldData> fields)
		{
			if (fields.Count == 0) return;

			// TODO: hoist inits into a method if there's more than 1 ctor?
			var hasCtor = false;
			foreach (var ctor in type.GetConstructors())
			{
				hasCtor = true;
				for (var i = fields.Count - 1; i >= 0; i--)
				{
					var field = fields[i];

					ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Stfld, field.BackingField));

					for (var j = field.InitCode.Length - 1; j >= 0; j--)
					{
						ctor.Body.Instructions.Insert(0, field.InitCode[j]);
					}

					ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
				}
			}

			if (!hasCtor) throw new Exception("No constructor O_o");
		}
	}

	static class HelperExtensions
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

		public static bool CheckAndRemoveCustomAttribute(this ICustomAttributeProvider provider, string attributeName)
		{
			if (!provider.HasCustomAttributes) return false;

			var removed = false;
			var attributes = provider.CustomAttributes;
			var i = 0;
			while (i < attributes.Count)
			{
				var attribute = attributes[i];
				if (attribute.AttributeType.FullName == attributeName)
				{
					attributes.RemoveAt(i);
					removed = true;
				}
				else
				{
					i++;
				}
			}
			return removed;
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