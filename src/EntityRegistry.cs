using System;
using System.Collections.Generic;

namespace SimpleECS
{
    class EntityRegistry
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
        Queue<int> freeIDs = new Queue<int>();
        int count = 0;

        public Entity RegisterEntity(ArchetypeContainer container, int index)
        {
            if (count == Locations.Length)
                Array.Resize(ref Locations, Locations.Length * 2);
            if (!freeIDs.TryDequeue(out int id))
            {
                id = count;
                count++;
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
            freeIDs.Enqueue(entity.Id);
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
    }
}
