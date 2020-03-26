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
            MethodInfo method = type.GetMethod(kernelName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException();

            DynamicMethod archetypeCaller = new DynamicMethod($"Archetype caller for {type.FullName}.{kernelName}",
                returnType: null,
                parameterTypes: new Type[] { typeof(ArchetypeContainer), typeof(T) },
                restrictedSkipVisibility: true);

            var il = archetypeCaller.GetILGenerator();
            il.EmitWriteLine("Kernel was called :-)");
            il.Emit(OpCodes.Ret);

            var callerDelegate = (ArchetypeKernelRunner<T>)archetypeCaller.CreateDelegate(typeof(ArchetypeKernelRunner<T>), null);

            return (scene, obj) =>
            {
                scene.UpdateArchetypes();
                Console.WriteLine(scene.archetypes.Count);
                foreach (var (set, archetype) in scene.archetypes)
                {
                    // TODO: check if we should run
                    bool shouldRun = true;
                    if (shouldRun)
                        callerDelegate(archetype, obj);
                }
                throw new NotImplementedException();
            };
        }
    }
}
