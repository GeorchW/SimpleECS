using System.Linq;
using System;
using System.Numerics;

namespace SimpleECS
{
    public readonly struct Entity
    {
        [ThreadStatic]
        internal static Scene CurrentScene = null!;

        public int Id { get; }
        public int Version { get; }
        public bool IsValid => CurrentScene.EntityRegistry.TryGetLocation(this, out _);

        /// <summary>
        /// Lists all components in this entity, excluding recently added ones.
        /// </summary>
        [Obsolete("Do not use this property for non-debugging purposes - performance might be poor and it might give incorrect results.")]
        public object[] Components
        {
            get
            {
                var loc = LocationOrThrow();
                return LocationOrThrow().ArchetypeContainer.arrays.Values.Select(array => array.GetValue(loc.Index)!).ToArray();
            }
        }

        internal Entity(int id, int version)
        {
            Id = id;
            Version = version;
        }

        EntityRegistry.EntityLocation LocationOrThrow()
        {
            if (!CurrentScene.EntityRegistry.TryGetLocation(this, out var location))
                throw new Exception("The requested entity has ceased to exist.");
            return location;
        }

        public ref T Add<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.Add<T>(loc.Index);
        }
        public void Remove<T>() where T : struct
        {
            var loc = LocationOrThrow();
            CurrentScene.Callbacks.Get(typeof(T)).OnComponentRemoved(CurrentScene, this);
            loc.ArchetypeContainer.Remove<T>(loc.Index);
        }
        public bool Has<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return loc.ArchetypeContainer.Has<T>(loc.Index);
        }
        public ref readonly T Get<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.Get<T>(loc.Index);
        }
        public ref T GetMutable<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.GetMutable<T>(loc.Index);
        }
        public ref T GetOrAdd<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.GetOrAdd<T>(loc.Index);
        }
        public void Delete() => CurrentScene.DeleteEntity(this);

        public override string ToString() => Has<NameComp>() ? Get<NameComp>().Name : $"EID {Id} (v{Version})";

        public override bool Equals(object? obj) => obj is Entity other && this == other;
        public override int GetHashCode() => (int)(BitOperations.RotateLeft((uint)Id, 16) ^ Version);
        public static bool operator ==(Entity left, Entity right) => left.Id == right.Id && left.Version == right.Version;
        public static bool operator !=(Entity left, Entity right) => !(left == right);
    }
}
