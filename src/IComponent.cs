using System;

namespace SimpleECS
{
    public interface IComponent { }
    public interface ISerializableComponent : IComponent { }
    static class ComponentHelpers
    {
        public static void AssertIsComponentType(Type type)
        {
            if (!typeof(IComponent).IsAssignableFrom(type))
                throw new Exception($"The type {type} does not implement the IComponent interface.");
            if (!type.IsValueType)
                throw new Exception($"Components must have value type. Type: {type}");
        }
    }
}
