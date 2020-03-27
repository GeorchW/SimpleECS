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
        int[] entityIds;
        public int EntityCount { get; private set; } = 0;
        int highestIndex = -1;
        int capacity = 64;

        Queue<int> freeSlots = new Queue<int>();

        Scene scene;

        internal ArchetypeContainer(Scene scene)
        {
            this.scene = scene;
            entityIds = new int[capacity];
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
            if (index == highestIndex)
                highestIndex--;
            else
                freeSlots.Enqueue(index);
        }

        internal int AddEntity(int id)
        {
            EntityCount++;
            if (!freeSlots.TryDequeue(out int index))
            {
                highestIndex++;
                EnsureCapacity();
                index = highestIndex;
            }

            entityIds[index] = id;

            return index;
        }

        public int GetEntityId(int index) => entityIds[index];

        private void EnsureCapacity()
        {
            bool increasedCapacity = false;
            while (highestIndex >= capacity)
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
                    scene.InsertNewComponents();

                arrays = arrays.ToDictionary(pair => pair.Key, pair =>
                {
                    var (type, array) = pair;
                    Array newArray = Array.CreateInstance(type, capacity);
                    array.CopyTo(newArray, 0);
                    return newArray;
                });
                Array.Resize(ref entityIds, capacity);
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
                    int swapTarget = highestIndex;
                    while (swapTarget == indices[^1])
                    {
                        indices = indices[0..^1];
                        highestIndex--;
                        if (indices.Length == 0)
                            return;
                    }

                    foreach (var (type, array) in arrays)
                    {
                        Array.Copy(array, highestIndex, array, index, 1);
                    }
                    foreach (var (type, array) in arrays)
                    {
                        Array.Clear(array, highestIndex, 1);
                    }
                    entityIds[index] = entityIds[highestIndex];
                    entityIds[highestIndex] = -1;

                    highestIndex--;
                }
            }
        }

        internal static void BlockCopy(ArchetypeContainer source, ArchetypeContainer target, out int targetStartIndex)
        {
            source.DoCompaction();
            target.DoCompaction();

            targetStartIndex = target.EntityCount;
            target.EntityCount += source.EntityCount;
            target.EnsureCapacity();
            foreach (var (type, sourceArray) in source.arrays)
            {
                var targetArray = target.GetArray(type);
                Array.Copy(sourceArray, 0, targetArray, targetStartIndex, source.EntityCount);
            }

            Array.Copy(source.entityIds, 0, target.entityIds, targetStartIndex, source.EntityCount);
        }
    }
}
