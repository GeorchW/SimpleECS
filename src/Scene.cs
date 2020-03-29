using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;

namespace SimpleECS
{
    public class Scene : IReadOnlyCollection<Entity>
    {
        internal Dictionary<ComponentSet.Readonly, ArchetypeContainer> archetypes = new Dictionary<ComponentSet.Readonly, ArchetypeContainer>();

        ArchetypeContainer InitialContainer { get; }

        internal EntityRegistry EntityRegistry { get; } = new EntityRegistry();
        public GlobalStorage Globals { get; } = new GlobalStorage();
        public CallbackManager Callbacks { get; } = new CallbackManager();

        public int Count => EntityRegistry.Count;
        public IEnumerator<Entity> GetEnumerator() => EntityRegistry.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Scene()
        {
            InitialContainer = new ArchetypeContainer(this);
            archetypes.Add(default, InitialContainer);
            Entity.CurrentScene = this;
        }

        public Entity CreateEntity()
        {
            var entity = EntityRegistry.RegisterEntity(InitialContainer, 0);
            int index = InitialContainer.AddEntity(entity.Id);
            EntityRegistry.MoveEntity(entity.Id, InitialContainer, index);
            return entity;
        }

        public void DeleteEntity(Entity entity)
        {
            if (!EntityRegistry.TryGetLocation(entity, out var location))
                throw new Exception("The entity is already deleted.");
            foreach (var type in location.ArchetypeContainer.arrays.Keys)
            {
                var callback = Callbacks.TryGet(type);
                if (callback != null && !location.ArchetypeContainer.removals.Contains((location.Index, type)))
                    callback?.OnComponentRemoved(this, entity);
            }
            foreach (var (index, type) in location.ArchetypeContainer.additions.Keys)
            {
                if (index == location.Index)
                {
                    Callbacks.TryGet(type)?.OnComponentRemoved(this, entity);
                }
            }
            EntityRegistry.UnregisterEntity(entity, out var lastLocation);
            lastLocation.ArchetypeContainer.RemoveEntity(lastLocation.Index);
        }

        // This is where new components (that are not yet in the correct archetype) are stored.
        Dictionary<Type, TempComponentStorage> tempStorage = new Dictionary<Type, TempComponentStorage>();
        class TempComponentStorage
        {
            public Type Type { get; }
            Array storage;
            int Capacity => storage.Length;
            int count = 0;

            // Use a big default capacity to avoid early re-allocations.
            // Realistically, we're not going to have more than 100 components with 100 bytes each,
            // making a total of 100*100*100 bytes = 1MB overhead, which is something we don't care about
            const int DEFAULT_CAPACITY = 100;

            public TempComponentStorage(Type type, int capacity = DEFAULT_CAPACITY)
            {
                Type = type;
                storage = Array.CreateInstance(type, capacity);
            }

            internal ArrayElementRef GetEmptyElement()
            {
                if (count >= Capacity)
                {
                    // No need to copy the elements, since the old array is referenced 
                    // by the individual ArrayElementRefs.
                    // We can just create a new array & reset the counter
                    storage = Array.CreateInstance(Type, Capacity * 2);
                    count = 0;
                }
                int index = count;
                count++;
                return new ArrayElementRef(storage, index);
            }
        }

        internal ArrayElementRef GetStorage(Type type)
        {
            if (!tempStorage.TryGetValue(type, out var container))
                tempStorage.Add(type, container = new TempComponentStorage(type));
            return container.GetEmptyElement();
        }

        internal bool IsUpdatingArchetypes { get; private set; } = false;
        public void InsertNewComponents()
        {
            IsUpdatingArchetypes = true;
            InsertNewComponentsInternal();
            IsUpdatingArchetypes = false;
        }

        void InsertNewComponentsInternal()
        {
            //TODO: This is not as efficient as it could be (allocations etc.). May have to optimize at some point.
            foreach (var (set, archetype) in archetypes)
            {
                Dictionary<int, Dictionary<Type, ArrayElementRef>> changedEntities = new Dictionary<int, Dictionary<Type, ArrayElementRef>>();

                Dictionary<Type, ArrayElementRef> GetNewComponentSet(int index)
                {
                    if (!changedEntities.TryGetValue(index, out var value))
                    {
                        value = new Dictionary<Type, ArrayElementRef>();
                        foreach (var (type, array) in archetype.arrays)
                        {
                            value.Add(type, new ArrayElementRef(array, index));
                        }
                        changedEntities.Add(index, value);
                    }
                    return value;
                }
                foreach (var (index, type) in archetype.removals)
                    GetNewComponentSet(index).Remove(type);
                archetype.removals.Clear();

                foreach (var ((index, type), value) in archetype.additions)
                    GetNewComponentSet(index).Add(type, value);
                archetype.additions.Clear();

                foreach (var (index, newComponents) in changedEntities)
                {
                    // Compute new set
                    ComponentSet newSet = default;
                    foreach (var type in newComponents.Keys)
                    {
                        newSet.Add(type);
                    }

                    if (!archetypes.TryGetValue(newSet.AsReadOnly(), out var newArchetype)
                    && !newArchetypes.TryGetValue(newSet.AsReadOnly(), out newArchetype))
                    {
                        newArchetype = CreateArchetype(newSet);
                    }

                    if (newArchetype == archetype)
                        continue;

                    int id = archetype.GetEntityId(index);
                    int newIndex = newArchetype.AddEntity(id);
                    foreach (var (type, component) in newComponents)
                    {
                        component.CopyTo(newArchetype.GetArray(type), newIndex);
                    }
                    archetype.RemoveEntity(index);
                    EntityRegistry.MoveEntity(id, newArchetype, newIndex);
                }
            }

            foreach (var (k, v) in newArchetypes)
            {
                archetypes.Add(k, v);
            }
            newArchetypes.Clear();
        }

        internal void UpdateArchetypeDictionary()
        {
            var archetypesCopy = archetypes.Values.ToArray();

            archetypes.Clear();
            foreach (var archetype in archetypesCopy)
            {
                ComponentSet set = default;
                foreach (var type in archetype.arrays.Keys)
                {
                    set.Add(type);
                }

                if (!archetypes.TryGetValue(set.AsReadOnly(), out var existingArchetype))
                {
                    archetypes.Add(set.AsReadOnly(), archetype);
                }
                else
                {
                    ArchetypeContainer.BlockCopy(archetype, existingArchetype, out int startIndex);
                    for (int i = 0; i < archetype.EntityCount; i++)
                    {
                        int entity = archetype.GetEntityId(i);
                        int newIndex = startIndex + i;
                        EntityRegistry.MoveEntity(entity, existingArchetype, newIndex);
                    }
                }
            }
        }

        Dictionary<ComponentSet.Readonly, ArchetypeContainer> newArchetypes = new Dictionary<ComponentSet.Readonly, ArchetypeContainer>();
        private ArchetypeContainer CreateArchetype(ComponentSet componentSet)
        {
            ArchetypeContainer? newArchetype;
            newArchetype = new ArchetypeContainer(this);
            foreach (var type in componentSet)
            {
                newArchetype.AddComponentToAllEntities(type);
            }
            ComponentSet copy = default;
            componentSet.AsReadOnly().CopyTo(ref copy);
            newArchetypes.Add(copy.AsReadOnly(), newArchetype);
            return newArchetype;
        }

        KernelRunner kernelRunner = new KernelRunner();
        public void Run<T>(T obj, string kernelName) => kernelRunner.Run(obj, kernelName, this);
    }
}
