using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator.IR
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MIRValueKind
    {
        Return,
        Constant,
        StaticMemoryAddress,
        Load,
        StoreLocal,
        StoreField,
        Dupplicate,
        LoadZeroinitialized,
        LoadField,
        LoadLocal
    }

    public struct MIRValue
    {
        public MIRValueKind Kind { get; }
        public MIRType? Type { get; }
        public object Value { get; }

        internal MIRValue(MIRValueKind kind, MIRType? type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal static MIRValue Constant(MIRType type, object value)
        {
            return new(MIRValueKind.Constant, type, value);
        }

        internal static MIRValue StaticMemoryAddress(int address, MIRType type)
        {
            return new MIRValue(MIRValueKind.StaticMemoryAddress, type, address);
        }

        internal ulong ConstantIntValue => (ulong)Value;
        internal MIRValue ParameterValue => (MIRValue)Value;

        public override string ToString()
        {
            return $"{Kind} => ({Value ?? "_"}{(Type.HasValue ? $" {Type}" : "")})";
        }
    }
}
