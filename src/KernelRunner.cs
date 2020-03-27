using System.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SimpleECS
{
    class KernelRunner
    {
        Dictionary<(Type, string), Delegate> kernelRunners = new Dictionary<(Type, string), Delegate>();
        delegate void SceneKernelRunner<T>(Scene scene, T obj);
        delegate void ArchetypeKernelRunner<T>(ArchetypeContainer archetypeContainer, T obj);
        public void Run<T>(T obj, string kernelName, Scene scene)
        {
            if (!kernelRunners.TryGetValue((typeof(T), kernelName), out var runner))
            {
                runner = CreateRunner<T>(kernelName);
                kernelRunners.Add((typeof(T), kernelName), runner);
            }
            var castedDelegate = (SceneKernelRunner<T>)runner;
            castedDelegate(scene, obj);
        }

        private SceneKernelRunner<T> CreateRunner<T>(string kernelName)
        {
            var type = typeof(T);
            MethodInfo kernel = type.GetMethod(kernelName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException();

            AssemblyBuilder asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SimpleECS.Dynamic"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = asm.DefineDynamicModule("SimpleECS.Dynamic (Module)");
            module.SetCustomAttribute(typeof(System.Security.UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes)!, new byte[0]);

            var il = Sigil.Emit<ArchetypeKernelRunner<T>>.NewDynamicMethod($"(* Archetype caller for {type.FullName}.{kernelName} *)", module);
            il.WriteLine($"Running kernel {kernelName}...");
            ParameterInfo[] kernelParameters = kernel.GetParameters();
            var requiredComponents = new List<Type>();
            var arrays = kernelParameters.Select(param =>
            {
                if (param.ParameterType.IsByRef)
                {
                    Type elementType = param.ParameterType.GetElementType()!;
                    requiredComponents.Add(elementType);
                    Type arrayType = elementType.MakeArrayType();
                    var local = il.DeclareLocal(arrayType);

                    il.LoadArgument(0);
                    il.LoadConstant(elementType);
                    il.Call(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                    il.Call(typeof(ArchetypeContainer).GetMethod(nameof(ArchetypeContainer.GetArray), BindingFlags.NonPublic | BindingFlags.Instance)!);
                    il.CastClass(arrayType);
                    il.StoreLocal(local);

                    return local;
                }
                else throw new NotImplementedException();
            }).ToArray();

            var length = il.DeclareLocal<int>("length");
            il.LoadArgument(0);
            il.Call(typeof(ArchetypeContainer).GetProperty(nameof(ArchetypeContainer.EntityCount))!.GetMethod);
            il.StoreLocal(length);

            il.For(length, i =>
            {
                il.LoadArgument(1);
                foreach (var array in arrays)
                {
                    il.LoadLocal(array);
                    il.LoadLocal(i);
                    il.LoadElementAddress(array.LocalType.GetElementType()!);
                }
                il.Call(kernel);
            });

            il.WriteLine($"Done with {kernelName}.");
            il.Return();

            var callerDelegate = il.CreateDelegate(out string instructions);

            return (scene, obj) =>
            {
                Entity.CurrentScene = scene;
                scene.UpdateArchetypes();
                foreach (var (set, archetype) in scene.archetypes)
                {
                    bool shouldRun = true;
                    foreach (var type in requiredComponents)
                    {
                        if (!set.Has(type))
                        {
                            shouldRun = false;
                            break;
                        }
                    }
                    // TODO: check if we should run
                    if (shouldRun)
                        callerDelegate(archetype, obj);
                }
            };
        }
    }
}
