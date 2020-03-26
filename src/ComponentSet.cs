using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleECS
{
    struct ComponentSet : IEquatable<ComponentSet>, IEnumerable<Type>
    {
        HashSet<Type>? components;
        int hash;

        public int Count => components?.Count ?? 0;
        public bool IsReadOnly => false;

        public void Clear()
        {
            hash = 0;
            components?.Clear();
        }
        public void Add(Type type)
        {
            if (components == null)
                components = new HashSet<Type>();
            components.Add(type);
            hash ^= type.GetHashCode();
        }
        public Readonly AsReadOnly() => new Readonly(this);

        public override int GetHashCode() => hash;

        public static bool operator ==(in ComponentSet left, in ComponentSet right)
        {
            if (left.hash != right.hash) return false;
            if (left.components == null || right.components == null)
            {
                int leftCount = left.components?.Count ?? 0;
                int rightCount = right.components?.Count ?? 0;
                return leftCount == 0 && rightCount == 0;
            }
            return left.components.SetEquals(right.components);
        }
        public static bool operator !=(in ComponentSet left, in ComponentSet right) => !(left == right);

        public override bool Equals(object? obj) => obj is ComponentSet other && this == other;

        public bool Equals(ComponentSet other) => this == other;

        public IEnumerator<Type> GetEnumerator() => components?.GetEnumerator() ?? Enumerable.Empty<Type>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => components?.GetEnumerator() ?? Enumerable.Empty<Type>().GetEnumerator();

        public override string ToString() => components != null ? $"{{{string.Join(", ", components)}}}" : "{}";

        public readonly struct Readonly : IEquatable<Readonly>
        {
            private readonly ComponentSet componentSet;

            public Readonly(ComponentSet componentSet) => this.componentSet = componentSet;

            public override bool Equals(object? obj)
            {
                return obj is Readonly other && other.componentSet == this.componentSet;
            }

            public bool Equals(Readonly other) => other.componentSet == this.componentSet;

            public override int GetHashCode() => componentSet.GetHashCode();

            internal void CopyTo(ref ComponentSet newSet)
            {
                newSet.Clear();
                foreach(var component in componentSet)
                    newSet.Add(component);
            }

            public override string ToString() => componentSet.ToString() + " (read-only)";
        }
    }
}
