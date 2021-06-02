using Mug.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
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
        Div = '/',
        Add = '+',
        Sub = '-',
        Mul = '*',
        Call = 50,
        Pop,
        JumpFalse,
        Jump,
        Ceq = -10,
        Neq = -11,
        Leq = -19,
        Geq = -20,
        Greater = '>',
        Less = '<',
        LoadValueFromPointer = 63,
        Neg = 64,
        Label = 65,
        StoreGlobal = 66,
        CastIntToInt = 67,
    }

    public struct MIRInstruction
    {
        public MIRInstructionKind Kind { get; internal set; }
        public MIRType Type { get; }
        public object Value { get; }

        internal MIRInstruction(MIRInstructionKind kind, MIRType type = default, object value = null)
        {
            Kind = kind;
            Type = type;
            Value = value;
        }

        public bool ConstantBoolValue => (bool)Value;
        internal long ConstantIntValue => (long)Value;
        internal MIRInstruction ParameterValue => (MIRInstruction)Value;

        public override string ToString()
        {
            if (Kind == MIRInstructionKind.Label)
                return $"~ {Value}:";

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

        public string GetName()
        {
            return (string)Value;
        }

        public MIRLabel GetLabel()
        {
            return (MIRLabel)Value;
        }
    }
}
