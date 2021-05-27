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
    public enum MIRInstructionKind
    {
        Return,
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
        Call = 50,
        Pop,
        JumpFalse,
        Jump,
        Ceq = -1,
        Neq = -11,
        Leq = -19,
        Geq = -20,
        Greater = '>',
        Less = '<',
        LoadValueFromPointer = 63
    }

    public struct MIRInstruction
    {
        public MIRInstructionKind Kind { get; internal set; }
        public DataType Type { get; }
        public object Value { get; }

        internal MIRInstruction(MIRInstructionKind kind, DataType type = null, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        internal long ConstantIntValue => (long)Value;
        internal MIRInstruction ParameterValue => (MIRInstruction)Value;

        public override string ToString()
        {
            if (Kind == MIRInstructionKind.Comment)
                return Value is not null ? $"~ {Value}" : "";

            return $"{Kind} {Type} ({Value ?? "_"})";
        }

        public bool IsIntConstant()
        {
            return Value is long;
        }

        public int GetStackIndex()
        {
            return (int)Value;
        }
    }
}
