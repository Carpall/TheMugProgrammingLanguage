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
        JumpConditional,
        Jump,
        Ceq = -10,
        Neq = -11,
        Leq = -19,
        Geq = -20,
        Greater = '>',
        Less = '<',
        LoadValueFromPointer = 63,
        Neg = 64,
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

        internal long ConstantIntValue => Type.GetIntBitSize() != 1 ? (long)Value : Convert.ToInt64(Value);

        internal MIRInstruction ParameterValue => (MIRInstruction)Value;

        public override string ToString()
        {
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

        public (int then, int otherwise) GetConditionTuple()
        {
            return ((int, int))Value;
        }

        public int GetInt()
        {
            return (int)Value;
        }
    }
}
