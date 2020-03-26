using System;
using System.Collections.Generic;

namespace SimpleECS
{
    class Scene
    {
        Dictionary<ComponentSet.Readonly, ArchetypeContainer> archetypes = new Dictionary<ComponentSet.Readonly, ArchetypeContainer>();

        ArchetypeContainer InitialContainer { get; }

        internal EntityRegistry EntityRegistry { get; } = new EntityRegistry();

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
        public void UpdateArchetypes()
        {
            IsUpdatingArchetypes = true;
            UpdateArchetypesInternal();
            IsUpdatingArchetypes = false;
        }

        void UpdateArchetypesInternal()
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
                foreach (var ((index, type), value) in archetype.additions)
                    GetNewComponentSet(index).Add(type, value);

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
    }
}
