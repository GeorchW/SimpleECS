using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleECS
{
    /// <summary>
    /// Allows observing specific types of components for changes.
    /// </summary>
    class ComponentObserver
    {
        public Type ObservedType { get; }

        public ComponentObserver(Type observedType) => ObservedType = observedType;

        Dictionary<ArchetypeContainer, HashSet<int>> observedContainers = new Dictionary<ArchetypeContainer, HashSet<int>>();
        /// <summary>
        /// Gets the set of all indices that have been changed since the last reset. Returns null if the entire container is dirty.
        /// </summary>
        /// <param name="container">The container for which to determine the changed components.</param>
        public HashSet<int>? GetDirtyIndices(ArchetypeContainer container) => observedContainers.GetValueOrDefault(container);
        /// <summary>
        /// Sets all entities of the selected container to be considered "clean", i.e. not dirty.
        /// </summary>
        public void Clear(ArchetypeContainer container) 
        {
            if (observedContainers.TryGetValue(container, out var result))
                result.Clear();
            else
            {
                observedContainers.Add(container, new HashSet<int>());
                
                if (!container.Observers.TryGetValue(ObservedType, out var observers))
                    container.Observers.Add(ObservedType, observers = new HashSet<ComponentObserver>());
                observers.Add(this);
            }
        }

        public void NotifyAllChanged(ArchetypeContainer container) => observedContainers.Remove(container);

        public void NotifyChangeOrAdd(ArchetypeContainer container, int location) => observedContainers[container].Add(location);

        public void TrackMove(ArchetypeContainer oldContainer, int oldLocation, ArchetypeContainer newContainer, int newLocation)
        {
            if (observedContainers.TryGetValue(oldContainer, out var oldChanges))
            {
                if(oldChanges.Remove(oldLocation))
                    observedContainers[newContainer].Add(newLocation);
            }
            else if(observedContainers.TryGetValue(newContainer, out var newChanges))
            {
                newChanges.Add(newLocation);
            }
        }
        public void TrackDelete(ArchetypeContainer oldContainer, int oldLocation) => observedContainers[oldContainer].Remove(oldLocation);
    }
}
