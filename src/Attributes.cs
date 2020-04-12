using System;
using System.Collections.ObjectModel;

namespace SimpleECS
{
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class BannedAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class BannedComponentAttribute : System.Attribute
    {
        public ReadOnlyCollection<Type> Components { get; }

        public BannedComponentAttribute(params Type[] components)
        {
            foreach (var component in components)
                ComponentHelpers.AssertIsComponentType(component);
            Components = new ReadOnlyCollection<Type>(components);
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class GlobalAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class ChangedAttribute : System.Attribute
    {
    }
}
