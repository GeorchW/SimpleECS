using System;

namespace SimpleECS
{
    readonly struct ArrayElementRef
    {
        readonly Array array;
        readonly int index;

        public ArrayElementRef(Array array, int index)
        {
            this.array = array;
            this.index = index;
        }
        public ref T Get<T>() where T : struct, IComponent => ref ((T[])array)[index];
        public void CopyTo(Array targetArray, int targetIndex) => Array.Copy(array, index, targetArray, targetIndex, 1);
        internal void Set(object component) => array.SetValue(component, index);
    }
}
