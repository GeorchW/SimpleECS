using System;

namespace SimpleECS
{
    readonly struct Entity
    {
        [ThreadStatic]
        internal static Scene CurrentScene = null!;

        public int Id { get; }
        public int Version { get; }
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
            loc.ArchetypeContainer.Remove<T>(loc.Index);
        }
        public bool Has<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return loc.ArchetypeContainer.Has<T>(loc.Index);
        }
        public ref T Get<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.Get<T>(loc.Index);
        }
        public ref T GetOrAdd<T>() where T : struct
        {
            var loc = LocationOrThrow();
            return ref loc.ArchetypeContainer.GetOrAdd<T>(loc.Index);
        }
        public void Delete() => CurrentScene.DeleteEntity(this);

        public override string ToString() => $"Entity {Id} (v{Version})";
    }
}
