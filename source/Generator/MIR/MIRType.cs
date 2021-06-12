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
        Void,
        UInt,
        Int,
        Struct,
        Pointer
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
            return Kind switch
            {
                MIRTypeKind.Struct => $"struct {GetStruct().Name}",
                MIRTypeKind.Pointer => $"{BaseType}*",
                _ => $"{Kind.ToString().ToLower()}{BaseType ?? ""}"
            };
        }

        public MIRType GetPointerBaseType()
        {
            return (MIRType)BaseType;
        }

        public bool IsStruct()
        {
            return Kind is MIRTypeKind.Struct;
        }
    }
}
