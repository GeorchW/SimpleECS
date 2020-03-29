using System;
using System.Collections;
using System.Collections.Generic;

namespace SimpleECS
{
    class EntityRegistry : IReadOnlyCollection<Entity>
    {
        public struct EntityLocation
        {
            public ArchetypeContainer ArchetypeContainer;
            public int Index;

            public EntityLocation(ArchetypeContainer archetypeContainer, int index)
            {
                ArchetypeContainer = archetypeContainer;
                Index = index;
            }
        }

        private const int InitialCapacity = 512;
        EntityLocation[] Locations = new EntityLocation[InitialCapacity];
        int[] Versions = new int[InitialCapacity];
        HashSet<int> freeIDs = new HashSet<int>();
        int nextId = 0;

        public int Count => nextId - freeIDs.Count;

        public int GetVersion(int entity) => Versions[entity];

        public Entity RegisterEntity(ArchetypeContainer container, int index)
        {
            if (nextId == Locations.Length)
                Array.Resize(ref Locations, Locations.Length * 2);
            int id;
            if (freeIDs.Count > 0)
            {
                // faster implementation of First()
                foreach (var _id in freeIDs) { id = _id; goto done; }
                throw new Exception();
            done:;
            }
            else
            {
                id = nextId;
                nextId++;
            }
            Locations[id] = new EntityLocation(container, index);
            return new Entity(id, Versions[id]);
        }
        public void UnregisterEntity(Entity entity, out EntityLocation lastLocation)
        {
            if (entity.Version != Versions[entity.Id])
                throw new Exception("The entity was already deleted.");
            lastLocation = Locations[entity.Id];
            Locations[entity.Id] = default;
            Versions[entity.Id]++;
            freeIDs.Add(entity.Id);
        }
        public void MoveEntity(int entity, ArchetypeContainer newContainer, int newIndex)
        {
            Locations[entity] = new EntityLocation(newContainer, newIndex);
        }
        public bool TryGetLocation(Entity entity, out EntityLocation location)
        {
            if (Versions[entity.Id] != entity.Version)
            {
                location = default;
                return false;
            }
            else
            {
                location = Locations[entity.Id];
                return true;
            }
        }

        public IEnumerator<Entity> GetEnumerator() => new EntityEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        struct EntityEnumerator : IEnumerator<Entity>
        {
            int currentId;
            EntityRegistry registry;
            public Entity Current => new Entity(currentId, registry.GetVersion(currentId));

            object? IEnumerator.Current => this.Current;

            public EntityEnumerator(EntityRegistry registry)
            {
                this.currentId = -1;
                this.registry = registry;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                do
                {
                    currentId++;
                    if (registry.nextId == currentId)
                        return false;
                }
                while (registry.freeIDs.Contains(currentId));
                return true;
            }

            public void Reset() => currentId = -1;
        }
    }
}
