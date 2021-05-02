using Mug.TypeSystem;
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
        Dupplicate,
        LoadZeroinitialized,
        LoadLocal,
        StoreLocal,
        LoadField,
        StoreField,
        Comment,
        Div,
        Add,
        Sub,
        Mul,
    }

    public struct MIRValue
    {
        public MIRValueKind Kind { get; internal set; }
        public MugType Type { get; }
        public object Value { get; }

        internal MIRValue(MIRValueKind kind, MugType type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal static MIRValue Constant(MugType type, object value)
        {
            return new(MIRValueKind.Constant, type, value);
        }

        internal static MIRValue StaticMemoryAddress(int address, MugType type)
        {
            return new MIRValue(MIRValueKind.StaticMemoryAddress, type, address);
        }

        internal ulong ConstantIntValue => (ulong)Value;
        internal MIRValue ParameterValue => (MIRValue)Value;

        public override string ToString()
        {
            if (Kind == MIRValueKind.Comment)
                return Value is not null ? $"~ {Value}" : "";

            return $"{Kind}: ({(Type is not null ? $"{Type} " : "")}{Value ?? "_"})";
        }
    }
}
