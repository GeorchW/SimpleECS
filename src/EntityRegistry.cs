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
        int[] entityArchetypes = new int[InitialCapacity];
        int[] entityIndices = new int[InitialCapacity];
        int[] Versions = new int[InitialCapacity];
        HashSet<int> freeIDs = new HashSet<int>();
        int nextId = 0;

        public int Count => nextId - freeIDs.Count;

        public int GetVersion(int entity) => Versions[entity];

        public Entity RegisterEntity(ArchetypeContainer container, int index)
        {
            if (nextId == entityArchetypes.Length)
            {
                int newSize = entityArchetypes.Length * 2;
                Array.Resize(ref entityArchetypes, newSize);
                Array.Resize(ref entityIndices, newSize);
                Array.Resize(ref Versions, newSize);
            }
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
            entityArchetypes[id] = container.ID;
            entityIndices[id] = index;
            Versions[id]++;
            return new Entity(id, Versions[id]);
        }
        public void UnregisterEntity(Entity entity, out EntityLocation lastLocation)
        {
            if (!IsValid(entity))
                throw new Exception("The entity was already deleted.");
            lastLocation = GetLocation(entity);
            entityArchetypes[entity.Id] = 0;
            entityIndices[entity.Id] = 0;
            Versions[entity.Id]++;
            freeIDs.Add(entity.Id);
        }
        public void MoveEntity(int entity, ArchetypeContainer newContainer, int newIndex)
        {
            entityArchetypes[entity] = newContainer.ID;
            entityIndices[entity] = newIndex;
        }
        public bool TryGetLocation(Entity entity, out EntityLocation location)
        {
            if (!IsValid(entity))
            {
                location = default;
                return false;
            }
            else
            {
                location = GetLocation(entity);
                return true;
            }
        }

        private bool IsValid(Entity entity) 
            => entity.Version != 0 && Versions[entity.Id] == entity.Version;

        private EntityLocation GetLocation(Entity entity) 
            => new EntityLocation(
                ArchetypeContainer.GetById(entityArchetypes[entity.Id])!, 
                entityIndices[entity.Id]);

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
