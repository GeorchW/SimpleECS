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
        delegate void ArchetypeKernelRunner<T>(ArchetypeContainer archetypeContainer, T obj, EntityRegistry registry);
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
            ParameterInfo[] kernelParameters = kernel.GetParameters();
            var requiredComponents = new List<Type>();
            var bannedComponents = new List<Type>();
            var createdComponents = new List<Type>();

            Action<Sigil.Local>[] paramLoaders = kernelParameters.Select(param =>
            {
                if (param.ParameterType == typeof(Entity))
                {
                    return (Action<Sigil.Local>)(index =>
                    {
                        // load ID
                        var id = il.DeclareLocal<int>("id");
                        il.LoadArgument(0);
                        il.LoadLocal(index);
                        il.Call(typeof(ArchetypeContainer).GetMethod(nameof(ArchetypeContainer.GetEntityId), BindingFlags.Public | BindingFlags.Instance));
                        il.StoreLocal(id);

                        // load version
                        var version = il.DeclareLocal<int>("version");
                        il.LoadArgument(2);
                        il.LoadLocal(id);
                        il.Call(typeof(EntityRegistry).GetMethod(nameof(EntityRegistry.GetVersion), BindingFlags.Public | BindingFlags.Instance));
                        il.StoreLocal(version);

                        il.WriteLine("Loaded entity {0} (v{1}) @ index {2}", id, version, index);

                        il.LoadLocal(id);
                        il.LoadLocal(version);
                        il.NewObject<Entity, int, int>();
                    });
                }
                else if (param.ParameterType.IsByRef)
                {
                    Type elementType = param.ParameterType.GetElementType()!;
                    if (elementType == typeof(Entity))
                        throw new Exception("An entity parameter cannot be used in combination with ref, in or out.");

                    bool banned = param.GetCustomAttribute(typeof(BannedAttribute)) != null;

                    if (!param.IsOut)
                    {
                        if (banned)
                            throw new Exception("Banned components must be marked with out.");
                        requiredComponents.Add(elementType);
                    }
                    else
                    {
                        if (banned)
                            bannedComponents.Add(elementType);
                        createdComponents.Add(elementType);
                    }
                    Type arrayType = elementType.MakeArrayType();
                    var array = il.DeclareLocal(arrayType);

                    il.LoadArgument(0);
                    il.LoadConstant(elementType);
                    il.Call(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                    il.Call(typeof(ArchetypeContainer).GetMethod(nameof(ArchetypeContainer.GetArray), BindingFlags.NonPublic | BindingFlags.Instance)!);
                    il.CastClass(arrayType);
                    il.StoreLocal(array);

                    return (Action<Sigil.Local>)(i =>
                    {
                        il.LoadLocal(array);
                        il.LoadLocal(i);
                        il.LoadElementAddress(elementType);
                    });
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
                foreach (var paramLoader in paramLoaders)
                    paramLoader(i);
                il.Call(kernel);
            });

            il.Return();

            var callerDelegate = il.CreateDelegate(out string instructions);

            return (scene, obj) =>
            {
                Entity.CurrentScene = scene;
                scene.InsertNewComponents();
                bool archetypeWasManipulated = false;
                foreach (var (set, archetype) in scene.archetypes)
                {
                    bool ShouldRun()
                    {
                        foreach (var type in requiredComponents)
                        {
                            if (!set.Has(type))
                                return false;
                        }
                        foreach (var type in bannedComponents)
                        {
                            if (set.Has(type))
                                return false;
                        }
                        return true;
                    }

                    if (ShouldRun())
                    {
                        foreach (var type in createdComponents)
                        {
                            if (!set.Has(type))
                            {
                                archetype.AddComponentToAllEntities(type);
                                archetypeWasManipulated = true;
                            }
                        }
                        callerDelegate(archetype, obj, scene.EntityRegistry);
                    }
                }
                if (archetypeWasManipulated)
                    scene.UpdateArchetypeDictionary();
            };
        }
    }
}
