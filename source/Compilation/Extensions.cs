using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Zap.Compilation
{
    internal unsafe struct MarshaledString : IDisposable
    {
        public MarshaledString(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
            {
                var value = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(value, 0, 0);

                Length = 0;
                Value = (sbyte*)value;
            }
            else
            {
                var valueBytes = Encoding.UTF8.GetBytes(input.ToString());
                var length = valueBytes.Length;
                var value = Marshal.AllocHGlobal(length + 1);
                Marshal.Copy(valueBytes, 0, value, length);
                Marshal.WriteByte(value, length, 0);

                Length = length;
                Value = (sbyte*)value;
            }
        }

        public int Length { get; private set; }

        public sbyte* Value { get; private set; }

        public void Dispose()
        {
            if (Value != null)
            {
                Marshal.FreeHGlobal((IntPtr)Value);
                Value = null;
                Length = 0;
            }
        }

        public static implicit operator sbyte*(in MarshaledString value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            var span = new ReadOnlySpan<byte>(Value, Length);
            return span.ToString();
        }
    }

    internal static class Extensions
    {
        public static string GetDescription(this Enum instance)
        {
            var genericEnumType = instance.GetType();
            var memberInfo = genericEnumType.GetMember(instance.ToString());

            if (memberInfo is not null && memberInfo.Length > 0)
            {
                var attr = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attr is not null && attr.Length > 0)
                    return ((DescriptionAttribute)attr.ElementAt(0)).Description;
            }

            return instance.ToString();
        }
    }
}