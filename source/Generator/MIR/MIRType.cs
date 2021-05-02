/*using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator.IR
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MIRTypeKind
    {
        Void,
        Int8,
        Int16,
        Int32,
        Int64,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        Float32,
        Float64,
        Float128,
        Pointer,
        Struct,
        StaticMemoryAdress
    }

    public interface IMIRTypeBase
    {
        [JsonIgnore]
        public int ByteSize { get; }

        public string Dump()
        {
            return ToString();
        }
    }

    public struct MIRStruct : IMIRTypeBase
    {
        public MIRType[] Body { get; }
        public int ByteSize
        {
            get
            {
                var result = 0;

                for (int i = 0; i < Body.Length; i++)
                    result += Body[i].ByteSize;

                return result;
            }
        }

        internal MIRStruct(MIRType[] body)
        {
            Body = body;
        }

        public override string ToString()
        {
            return $"{{ {string.Join(", ", Body)} }}";
        }
    }

    public struct MIRType : IMIRTypeBase
    {
        public MIRTypeKind Kind { get; }
        [JsonIgnore]
        public IMIRTypeBase Base { get; }
        public int ByteSize => Kind switch
        {
            MIRTypeKind.Void => 0,
            MIRTypeKind.Int8 or
            MIRTypeKind.UInt8 => 1,
            MIRTypeKind.Int16 or
            MIRTypeKind.UInt16 => 2,
            MIRTypeKind.Int32 or
            MIRTypeKind.UInt32 or
            MIRTypeKind.Float32 => 4,
            MIRTypeKind.Int64 or
            MIRTypeKind.UInt64 or
            MIRTypeKind.Float64 => 8,
            MIRTypeKind.Float128 => 16,
            MIRTypeKind.Pointer => Environment.Is64BitProcess ? 64 : 32,
            MIRTypeKind.Struct => Base.ByteSize
        };

        internal MIRType(MIRTypeKind kind, IMIRTypeBase basetype = null)
        {
            Kind = kind;
            Base = basetype;
        }

        public override string ToString()
        {
            return $"{Kind}{(Base is not null ? $" -> {Base.Dump()}" : "")}";
        }
    }
}
*/