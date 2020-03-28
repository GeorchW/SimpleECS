using System;
using System.Collections.Generic;

namespace SimpleECS
{
    public class CallbackManager
    {
        public class CallbackContainer
        {
            public Type ComponentType { get; }
            public event EventHandler<Entity>? ComponentRemoved;

            internal CallbackContainer(Type componentType) => ComponentType = componentType;
            internal void OnComponentRemoved(Scene scene, Entity entity) => ComponentRemoved?.Invoke(scene, entity);
        }
        Dictionary<Type, CallbackContainer> removeCallbacks = new Dictionary<Type, CallbackContainer>();
        internal CallbackManager() {}
        public CallbackContainer Get(Type componentType)
        {
            if (!removeCallbacks.TryGetValue(componentType, out var container))
                removeCallbacks.Add(componentType, container = new CallbackContainer(componentType));
            return container;
        }

        internal CallbackContainer? TryGet(Type componentType) => removeCallbacks.GetValueOrDefault(componentType);
    }
}
