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
        Load,
        Dupplicate,
        LoadZeroinitialized,
        LoadLocal,
        StoreLocal,
        LoadField,
        StoreField,
        Comment,
        Div = '/',
        Add = '+',
        Sub = '-',
        Mul = '*',
        Call,
        Pop,
        JumpFalse,
        Jump,
        Ceq = -1,
        Neq = -11,
        Leq = -19,
        Geq = -20,
        Greater = '>',
        Less = '<'
    }

    public struct MIRValue
    {
        public MIRValueKind Kind { get; internal set; }
        public DataType Type { get; }
        public object Value { get; }

        internal MIRValue(MIRValueKind kind, DataType type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal long ConstantIntValue => (long)Value;
        internal MIRValue ParameterValue => (MIRValue)Value;

        public override string ToString()
        {
            if (Kind == MIRValueKind.Comment)
                return Value is not null ? $"~ {Value}" : "";

            return $"{Kind} {Type} ({Value ?? "_"})";
        }

        public bool IsIntConstant()
        {
            return Value is long;
        }
    }
}
