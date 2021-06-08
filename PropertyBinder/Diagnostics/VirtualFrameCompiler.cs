using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using PropertyBinder.Engine;

#if !NETSTANDARD
using System.Diagnostics.SymbolStore;
#endif

namespace PropertyBinder.Diagnostics
{
    internal static class VirtualFrameCompiler
    {
        private const string ModuleName = "PropertyBinder.VirtualFrames.dll";

        private static readonly AssemblyBuilder Assembly;
        private static readonly ModuleBuilder Module;
        private static readonly HashSet<string> ClassNames = new HashSet<string>();

        static VirtualFrameCompiler()
        {
            var assemblyName = new AssemblyName("BINDING ");
#if NETSTANDARD
            Assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            Module = Assembly.DefineDynamicModule(ModuleName);
#else
            Assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            Module = Assembly.DefineDynamicModule(ModuleName, true);
#endif
        }

        internal static void TakeSnapshot(string assemblyPath)
        {
            if (File.Exists(assemblyPath))
            {
                File.Delete(assemblyPath);
            }

#if NETSTANDARD
            var generator = new Lokad.ILPack.AssemblyGenerator();
            generator.GenerateAssembly(Module.Assembly, assemblyPath);
#else
            Assembly.Save(Path.GetFileName(assemblyPath));
#endif
        }

        internal static void TakeSnapshot()
        {
            TakeSnapshot(ModuleName);
        }

        public static Action<BindingReference[], int> CreateMethodFrame(string description, StackFrame frame)
        {
            lock (Module)
            {
                const string methodName = " ";
                var className = description;
                if (ClassNames.Contains(className))
                {
                    className += " /" + ClassNames.Count;
                }

                ClassNames.Add(className);

                var type = Module.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);
                var method = type.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] {typeof(BindingReference[]), typeof(int)});
                method.SetImplementationFlags(MethodImplAttributes.NoOptimization);

                var il = method.GetILGenerator();

                var binding = il.DeclareLocal(typeof(BindingReference));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldelem, typeof(BindingReference));
                il.Emit(OpCodes.Stloc_S, binding);

                var fileName = frame?.GetFileName();
                if (!string.IsNullOrEmpty(fileName))
                {
#if !NETSTANDARD
                    var symbolDocument = Module.DefineDocument(fileName, SymDocumentType.Text, SymLanguageType.CSharp, SymLanguageVendor.Microsoft);
                    il.MarkSequencePoint(symbolDocument, frame.GetFileLineNumber(), frame.GetFileColumnNumber(), frame.GetFileLineNumber(), frame.GetFileColumnNumber() + 2);
#endif

                    method.DefineParameter(1, ParameterAttributes.None, "bindings");
                    method.DefineParameter(2, ParameterAttributes.None, "index");
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);

                var lblFin = il.DefineLabel();
                il.Emit(OpCodes.Bge_S, lblFin);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldelema, typeof(BindingReference));
                il.Emit(OpCodes.Call, GetPropertyOrThrow(typeof(BindingReference), nameof(BindingReference.DebugContext)).GetGetMethod());
                il.Emit(OpCodes.Callvirt, GetPropertyOrThrow(typeof(DebugContext), nameof(DebugContext.VirtualFrame)).GetGetMethod());
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Callvirt, GetMethodOrThrow(typeof(Action<BindingReference[], int>), nameof(Action<BindingReference[], int>.Invoke)));
                il.Emit(OpCodes.Ret);
                il.MarkLabel(lblFin);
                il.Emit(OpCodes.Ldloca_S, binding);
                il.Emit(OpCodes.Call, GetMethodOrThrow(typeof(BindingReference), nameof(BindingReference.Execute)));
                il.Emit(OpCodes.Ret);

                if (!string.IsNullOrEmpty(fileName))
                {
#if !NETSTANDARD
                    binding.SetLocalSymInfo("binding", 0, il.ILOffset);
#endif
                }

                var actualType = type.CreateType();
                return (Action<BindingReference[], int>) GetMethodOrThrow(actualType, methodName).CreateDelegate(typeof(Action<BindingReference[], int>));
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void SampleFrame(BindingReference[] bindings, int index)
        {
            var binding = bindings[index];
            if (index < bindings.Length + 1)
            {
                bindings[index + 1].DebugContext.VirtualFrame(bindings, index + 1);
                return;
            }

            binding.Execute();
        }

        private static PropertyInfo GetPropertyOrThrow(Type type, string propertyName)
        {
            return type.GetProperty(propertyName) ?? throw new ApplicationException($"Failed to get property {propertyName} on type {type}");
        }

        private static MethodInfo GetMethodOrThrow(Type type, string methodName)
        {
            return type.GetMethod(methodName) ?? throw new ApplicationException($"Failed to get method {methodName} on type {type}");
        }
    }
}