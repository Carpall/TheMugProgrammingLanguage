using Mug.Parser.AST.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public enum MIRTypeKind
    {
        UInt,
        Int,
        Void,
        Struct
    }

    public struct MIRType
    {
        public MIRTypeKind Kind { get; }
        public object BaseType { get; }

        public MIRType(MIRTypeKind kind, object basetype = null)
        {
            Kind = kind;
            BaseType = basetype;
        }

        public bool IsVoid()
        {
            return Kind is MIRTypeKind.Void;
        }

        public bool IsSignedInt()
        {
            return Kind is MIRTypeKind.Int;
        }

        public bool IsInt()
        {
            return Kind is MIRTypeKind.Int or MIRTypeKind.UInt;
        }

        public MIRStructure GetStruct()
        {
            return (MIRStructure)BaseType;
        }

        public int GetIntBitSize()
        {
            return (int)BaseType;
        }

        public override string ToString()
        {
            if (Kind is MIRTypeKind.Struct)
                return $"struct {GetStruct().Name}";

            return $"{Kind.ToString().ToLower()}{BaseType ?? ""}";
        }
    }
}
