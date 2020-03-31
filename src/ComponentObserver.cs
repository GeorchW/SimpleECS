using System.Collections.Generic;

namespace SimpleECS
{
    class ComponentObserver
    {
        Dictionary<ArchetypeContainer, HashSet<int>> observedContainers = new Dictionary<ArchetypeContainer, HashSet<int>>();
        public HashSet<int>? RequestChanges(ArchetypeContainer container)
        {
            if (observedContainers.TryGetValue(container, out var result))
                return result;
            else
            {
                observedContainers.Add(container, new HashSet<int>());
                //TODO: also register with the archetype
                return null;
            }
        }

        // TODO: Also notify when a component is created
        public void Notify(ArchetypeContainer container, int location) => observedContainers[container].Add(location);

        public void TrackMove(ArchetypeContainer oldContainer, int oldLocation, ArchetypeContainer newContainer, int newLocation)
        {
            if (observedContainers[oldContainer].Remove(oldLocation))
                observedContainers[newContainer].Add(newLocation);
        }
        public void TrackDelete(ArchetypeContainer oldContainer, int oldLocation)
        {
            observedContainers[oldContainer].Remove(oldLocation);
        }
    }
}
