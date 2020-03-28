using System;
using System.Collections.Generic;

namespace SimpleECS
{
    public class GlobalStorage
    {
        Dictionary<Type, object> globals = new Dictionary<Type, object>();
        public void Add(object obj) => globals.Add(obj.GetType(), obj);
        public T Get<T>() => (T)globals[typeof(T)];
        public bool Has<T>() => globals.ContainsKey(typeof(T));
        public void Remove<T>()
        {
            bool success = globals.Remove(typeof(T));
            if (!success) throw new KeyNotFoundException();
        }
    }
}
