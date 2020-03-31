using System;
using System.Collections.Generic;

namespace SimpleECS
{
    class ComponentObserver
    {
        public Type ObservedType { get; }

        public ComponentObserver(Type observedType) => ObservedType = observedType;

        Dictionary<ArchetypeContainer, HashSet<int>> observedContainers = new Dictionary<ArchetypeContainer, HashSet<int>>();
        public HashSet<int>? RequestChanges(ArchetypeContainer container)
        {
            if (observedContainers.TryGetValue(container, out var result))
                return result;
            else
            {
                observedContainers.Add(container, new HashSet<int>());
                
                if (!container.Observers.TryGetValue(ObservedType, out var observers))
                    container.Observers.Add(ObservedType, observers = new HashSet<ComponentObserver>());
                observers.Add(this);

                return null;
            }
        }

        public void NotifyAllChanged(ArchetypeContainer container) => observedContainers.Remove(container);

        public void NotifyChangeOrAdd(ArchetypeContainer container, int location) => observedContainers[container].Add(location);

        public void TrackMove(ArchetypeContainer oldContainer, int oldLocation, ArchetypeContainer newContainer, int newLocation)
        {
            if (observedContainers[oldContainer].Remove(oldLocation))
                observedContainers[newContainer].Add(newLocation);
        }
        public void TrackDelete(ArchetypeContainer oldContainer, int oldLocation) => observedContainers[oldContainer].Remove(oldLocation);
    }
}
