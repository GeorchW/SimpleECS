using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

namespace SimpleECS
{
    class ArchetypeContainer
    {
        static ConcurrentDictionary<int, WeakReference<ArchetypeContainer>> archetypesById = new ConcurrentDictionary<int, WeakReference<ArchetypeContainer>>();

        public static ArchetypeContainer? GetById(int id)
        {
            if (archetypesById.TryGetValue(id, out var weakReference))
            {
                if (weakReference.TryGetTarget(out var target))
                    return target;
                else
                    archetypesById.Remove(id, out _);
            }
            return null;
        }
        public int ID { get; }
        static int highestArchetypeId;

        internal Dictionary<Type, Array> arrays = new Dictionary<Type, Array>();
        internal Dictionary<(int, Type), ArrayElementRef> additions = new Dictionary<(int, Type), ArrayElementRef>();
        internal HashSet<(int, Type)> removals = new HashSet<(int, Type)>();
        int[] entityIds;
        public int EntityCount { get; private set; } = 0;
        int highestIndex = -1;
        int capacity = 64;

        internal Dictionary<Type, HashSet<ComponentObserver>> Observers = new Dictionary<Type, HashSet<ComponentObserver>>();

        Queue<int> freeSlots = new Queue<int>();

        Scene scene;

        internal ArchetypeContainer(Scene scene)
        {
            ID = Interlocked.Increment(ref highestArchetypeId);
            if (!archetypesById.TryAdd(ID, new WeakReference<ArchetypeContainer>(this)))
                throw new Exception();

            this.scene = scene;
            entityIds = new int[capacity];
        }

        public override string ToString()
        {
            var fullNames = arrays.Keys.Select(x => x.FullName).OrderBy(x => x);
            var fullNamesHash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', fullNames)));
            var fullNamesHashHex = BitConverter.ToUInt16(fullNamesHash.AsSpan()[..2]).ToString("x4");

            return $"ArchetypeContainer {ID} (type hash {fullNamesHashHex}), types: {string.Join(", ", arrays.Keys.Select(x => x.Name))}";
        }

        public void AddComponentToAllEntities(Type componentType)
        {
            ComponentHelpers.AssertIsComponentType(componentType);
            arrays.Add(componentType, Array.CreateInstance(componentType, capacity));
        }

        public void RemoveComponentFromAllEntities(Type componentType)
        {
            bool success = arrays.Remove(componentType);
            if (!success)
                throw new Exception("The component is not present on this archetype.");
        }

        public ref T Add<T>(int index) where T : struct, IComponent
        {
            if (arrays.ContainsKey(typeof(T)))
            {
                if (removals.Remove((index, typeof(T))))
                {
                    Notify<T>(index);
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

        internal void Add(int index, object component)
        {
            Type type = component.GetType();
            ComponentHelpers.AssertIsComponentType(type);
            if (arrays.ContainsKey(type))
            {
                if (removals.Remove((index, type)))
                {
                    Notify(type, index);
                    arrays[type].SetValue(component, index);
                }
                else throw new Exception("Component already exists");
            }
            else
            {
                var slot = scene.GetStorage(type);
                additions.Add((index, type), slot);
                slot.Set(component);
            }
        }

        public void Remove<T>(int index) where T : struct, IComponent
        {
            if (additions.Remove((index, typeof(T))))
                return;
            else if (arrays.ContainsKey(typeof(T)))
            {
                NotifyDeleteComponent<T>(index);
                bool success = removals.Add((index, typeof(T)));
                if (!success)
                    throw new Exception("No such component is present; it was recently removed.");
            }
            else
                throw new Exception("No such component is present");

        }
        public bool Has<T>(int index) where T : struct, IComponent
        {
            if (arrays.ContainsKey(typeof(T)))
                return !removals.Contains((index, typeof(T)));
            else
                return additions.ContainsKey((index, typeof(T)));
        }
        public ref readonly T Get<T>(int index) where T : struct, IComponent => ref GetInternal<T>(index);
        public ref T GetMutable<T>(int index) where T : struct, IComponent
        {
            // Call GetInternal first to validate input
            ref var result = ref GetInternal<T>(index);
            Notify<T>(index);
            return ref result;
        }
        ref T GetInternal<T>(int index) where T : struct, IComponent
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
        public ref T GetOrAdd<T>(int index) where T : struct, IComponent
        {
            if (arrays.TryGetValue(typeof(T), out var array))
            {
                removals.Remove((index, typeof(T)));
                Notify<T>(index);
                return ref ((T[])array)[index];
            }
            if (!additions.TryGetValue((index, typeof(T)), out var slot))
            {
                slot = scene.GetStorage(typeof(T));
                additions.Add((index, typeof(T)), slot);
            }
            return ref slot.Get<T>();
        }

        internal static void NotifyMove(ArchetypeContainer oldContainer, int oldIndex, ArchetypeContainer newContainer, int newIndex)
        {
            foreach (var observers in oldContainer.Observers.Values)
            {
                foreach (var observer in observers)
                    observer.TrackMove(oldContainer, oldIndex, newContainer, newIndex);
            }
        }
        internal void NotifyDeleteEntity(int index)
        {
            foreach (var observers in Observers.Values)
                foreach (var observer in observers)
                    observer.TrackDelete(this, index);
        }

        internal void NotifyDeleteComponent<T>(int index) where T : struct, IComponent
        {
            if (Observers.TryGetValue(typeof(T), out var observers))
                foreach (var observer in observers)
                    observer.TrackDelete(this, index);
        }

        private void Notify<T>(int index) where T : struct, IComponent => Notify(typeof(T), index);
        internal void Notify(Type changed, int index)
        {
            if (Observers.TryGetValue(changed, out var observers))
                foreach (var observer in observers)
                    observer.NotifyChangeOrAdd(this, index);
        }

        internal void NotifyAll(Type type)
        {
            if (Observers.TryGetValue(type, out var observers))
                foreach (var observer in observers)
                    observer.NotifyAllChanged(this);
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
                //TODO: Use Span.Sort when available in .NET 5.0
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
                    NotifyMove(this, highestIndex, this, index);

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

            HashSet<ComponentObserver> sourceObservers = CollectObservers(source);
            HashSet<ComponentObserver> targetObservers = CollectObservers(target);

            HashSet<ComponentObserver> sharedObservers = new HashSet<ComponentObserver>(sourceObservers);
            sharedObservers.IntersectWith(targetObservers);

            foreach (var observer in sharedObservers)
            {
                for (int i = 0; i < source.EntityCount; i++)
                {
                    NotifyMove(source, i, target, i + targetStartIndex);
                }
            }

            HashSet<ComponentObserver> exclusiveTargetObservers = new HashSet<ComponentObserver>(targetObservers);
            exclusiveTargetObservers.ExceptWith(sourceObservers);
            foreach (var observer in exclusiveTargetObservers)
            {
                for (int i = 0; i < source.EntityCount; i++)
                {
                    NotifyMove(source, i, target, i + targetStartIndex);
                }
            }
        }

        private static HashSet<ComponentObserver> CollectObservers(ArchetypeContainer source)
        {
            HashSet<ComponentObserver> sourceObservers = new HashSet<ComponentObserver>();
            foreach (var observers in source.Observers.Values)
                foreach (var observer in observers)
                    sourceObservers.Add(observer);
            return sourceObservers;
        }
    }
}
