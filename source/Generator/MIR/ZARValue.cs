using Zap.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator.IR
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ZARValueKind
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

    public struct ZARValue
    {
        public ZARValueKind Kind { get; internal set; }
        public ZapType Type { get; }
        public object Value { get; }

        internal ZARValue(ZARValueKind kind, ZapType type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal static ZARValue Constant(ZapType type, object value)
        {
            return new(ZARValueKind.Constant, type, value);
        }

        internal static ZARValue StaticMemoryAddress(int address, ZapType type)
        {
            return new(ZARValueKind.StaticMemoryAddress, type, address);
        }

        internal static ZARValue MemberIdentifier(string name, ZapType type)
        {
            return new(ZARValueKind.MemberIdentifer, type, name);
        }

        internal long ConstantIntValue => (long)Value;
        internal ZARValue ParameterValue => (ZARValue)Value;

        public override string ToString()
        {
            if (Kind == ZARValueKind.Comment)
                return Value is not null ? $"~ {Value}" : "";

            return $"{Kind}: ({(Type is not null ? $"{Type} " : "")}{Value ?? "_"})";
        }

        public bool IsIntConstant()
        {
            return Value is long;
        }
    }
}
