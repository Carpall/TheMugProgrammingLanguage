using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Compilation
{
    unsafe struct Reference<T> where T : unmanaged
    {
        private readonly IntPtr _pointer;

        unsafe private Reference(T* value)
        {
            _pointer = new(value);
        }

        unsafe public T Get() => *(T*)_pointer.ToPointer();

        unsafe public static Reference<T> MakeReference<T>(T* value) where T: unmanaged => new(value);
    }
}
