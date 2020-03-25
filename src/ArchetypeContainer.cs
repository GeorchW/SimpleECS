using System.Linq;
using System.Collections.Generic;
using System;

namespace SimpleECS
{
    class ArchetypeContainer
    {
        internal Dictionary<Type, Array> arrays = new Dictionary<Type, Array>();
        internal Dictionary<(int, Type), ArrayElementRef> additions = new Dictionary<(int, Type), ArrayElementRef>();
        internal HashSet<(int, Type)> removals = new HashSet<(int, Type)>();
        public int EntityCount { get; private set; } = 0;
        int highestEntityId = -1;
        int capacity = 64;

        Queue<int> freeSlots = new Queue<int>();

        Scene scene;

        internal ArchetypeContainer(Scene scene)
        {
            this.scene = scene;
        }

        public void AddComponentToAllEntities(Type componentType)
        {
            arrays.Add(componentType, Array.CreateInstance(componentType, capacity));
        }

        public void RemoveComponentFromAllEntities(Type componentType)
        {
            bool success = arrays.Remove(componentType);
            if (!success)
                throw new Exception("The component is not present on this archetype.");
        }

        public ref T Add<T>(int index) where T : struct
        {
            if (arrays.ContainsKey(typeof(T)))
            {
                if (removals.Remove((index, typeof(T))))
                {
                    return ref ((T[])arrays[typeof(T)])[index];
                }
                else throw new Exception("Component already exists");
            }
            else
            {
                var slot = scene.GetStorage(typeof(T));
                additions.Add((index, typeof(T)), slot);
                return ref slot.Get<T>();
            }
        }
        public void Remove<T>(int index) where T : struct
        {
            if (additions.Remove((index, typeof(T))))
                return;
            else if (arrays.ContainsKey(typeof(T)))
            {
                bool success = removals.Add((index, typeof(T)));
                throw new Exception("No such component is present; it was recently removed.");
            }
            else
                throw new Exception("No such component is present");

        }
        public bool Has<T>(int index) where T : struct
        {
            if (arrays.ContainsKey(typeof(T)))
                return !removals.Contains((index, typeof(T)));
            else
                return additions.ContainsKey((index, typeof(T)));
        }
        public ref T Get<T>(int index) where T : struct
        {
            if (arrays.TryGetValue(typeof(T), out var array))
            {
                if (removals.Contains((index, typeof(T))))
                    throw new Exception("No such component is present; it was recently deleted.");
                else
                    return ref ((T[])array)[index];
            }
            else if (additions.TryGetValue((index, typeof(T)), out var slot))
                return ref slot.Get<T>();
            else
                throw new Exception("No such component is present");
        }
        public ref T GetOrAdd<T>(int index) where T : struct
        {
            if (arrays.TryGetValue(typeof(T), out var array))
            {
                removals.Remove((index, typeof(T)));
                return ref ((T[])array)[index];
            }
            else if (additions.TryGetValue((index, typeof(T)), out var slot))
                return ref slot.Get<T>();
            else
                throw new Exception("No such component is present");
        }

        internal void RemoveEntity(int index)
        {
            EntityCount--;
            if (index == highestEntityId)
                highestEntityId--;
            else
                freeSlots.Enqueue(index);
        }

        internal int AddEntity()
        {
            EntityCount++;
            if (freeSlots.TryDequeue(out int index))
                return index;
            else
            {
                highestEntityId++;
                EnsureCapacity();
                return highestEntityId;
            }
        }

        private void EnsureCapacity()
        {
            bool increasedCapacity = false;
            while (highestEntityId >= capacity)
            {
                capacity *= 2;
                increasedCapacity = true;
            }
            if (increasedCapacity)
            {
                // Ensure that no ArrayElementRef's are still existing
                // referencing the original array.
                // (When the scene is already updating the archetypes, 
                // that's also fine, since then it's guranteed that we
                // won't have any writes into the old array that will 
                // end up in the void soon.)
                if (!scene.IsUpdatingArchetypes)
                    scene.UpdateArchetypes();

                arrays = arrays.ToDictionary(pair => pair.Key, pair =>
                {
                    var (type, array) = pair;
                    Array newArray = Array.CreateInstance(type, capacity);
                    array.CopyTo(newArray, 0);
                    return newArray;
                });
            }
        }

        internal Array GetArray(Type type) => arrays[type];

        internal void DoCompaction()
        {
            if (freeSlots.Count > 0)
            {
                var indicesArray = freeSlots.ToArray();
                Array.Sort(indicesArray);
                var indices = new Span<int>(indicesArray);
                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i];
                    int swapTarget = highestEntityId;
                    while (swapTarget == indices[^1])
                    {
                        indices = indices[0..^1];
                        highestEntityId--;
                        if (indices.Length == 0)
                            return;
                    }

                    foreach (var (type, array) in arrays)
                    {
                        Array.Copy(array, highestEntityId, array, index, 1);
                    }
                    foreach (var (type, array) in arrays)
                    {
                        Array.Clear(array, highestEntityId, 1);
                    }
                    highestEntityId--;
                }
            }
        }
    }
}
