using Nylon.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Models.Generator.IR
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NIRValueKind
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
        Call,
        MemberIdentifer,
        Pop,
    }

    public struct NIRValue
    {
        public NIRValueKind Kind { get; internal set; }
        public DataType Type { get; }
        public object Value { get; }

        internal NIRValue(NIRValueKind kind, DataType type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal static NIRValue Constant(DataType type, object value)
        {
            return new(NIRValueKind.Constant, type, value);
        }

        internal static NIRValue StaticMemoryAddress(int address, DataType type)
        {
            return new(NIRValueKind.StaticMemoryAddress, type, address);
        }

        internal static NIRValue MemberIdentifier(string name, DataType type)
        {
            return new(NIRValueKind.MemberIdentifer, type, name);
        }

        internal long ConstantIntValue => (long)Value;
        internal NIRValue ParameterValue => (NIRValue)Value;

        public override string ToString()
        {
            if (Kind == NIRValueKind.Comment)
                return Value is not null ? $"~ {Value}" : "";

            return $"{Kind}: ({(Type is not null ? $"{Type} " : "")}{Value ?? "_"})";
        }

        public bool IsIntConstant()
        {
            return Value is long;
        }
    }
}
