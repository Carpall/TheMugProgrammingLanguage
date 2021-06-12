using Mug.Compilation;
using Mug.Tokenizer;
using Mug.Parser;
using Mug.Symbols;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mug.Parser.AST.Statements;

namespace Mug.TypeSystem
{
    public struct SolvedType
    {
        public TypeKind Kind { get; set; }
        [JsonIgnore]
        public object Base { get; set; }
        public string BaseReppresentation => Base is not null ? Base.ToString() : "";

        public static SolvedType Struct(TypeStatement symbol)
        {
            return new SolvedType { Kind = TypeKind.DefinedType, Base = symbol };
        }

        public static SolvedType WithBase(TypeKind kind, DataType baseElementType)
        {
            return new SolvedType { Kind = kind, Base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public static SolvedType EnumError(DataType errorType, DataType successType)
        {
            return new SolvedType { Kind = TypeKind.EnumError, Base = (errorType, successType) };
        }

        public TypeStatement GetStruct()
        {
            return Base as TypeStatement;
        }

        public bool IsStruct()
        {
            return Kind == TypeKind.DefinedType || Kind == TypeKind.GenericDefinedType;
        }

        public DataType GetBaseElementType()
        {
            return (DataType)Base;
        }

        public bool IsArray()
        {
            return Kind == TypeKind.Array;
        }

        public (DataType ErrorType, DataType SuccessType) GetEnumError()
        {
            return ((DataType, DataType))Base;
        }

        public override string ToString()
        {
            return UnsolvedType.TypeKindToString(
                Kind,
                Base is not null ? Base is ISymbol symbol ? symbol.ToString() : Base.ToString() : "",
                Kind == TypeKind.EnumError ? (GetEnumError().ToString(), GetEnumError().ToString()) : new());
        }

        public bool IsInt()
        {
            return
                Kind is TypeKind.Int8
                or TypeKind.Int16
                or TypeKind.Int32
                or TypeKind.Int64
                or TypeKind.UInt8
                or TypeKind.UInt16
                or TypeKind.UInt32
                or TypeKind.UInt64;
        }

        public bool IsNewOperatorAllocable()
        {
            return
                Kind is TypeKind.GenericDefinedType
                or TypeKind.DefinedType;
        }

        public bool IsPointer()
        {
            return Kind == TypeKind.Pointer;
        }

        public bool IsVoid()
        {
            return Kind == TypeKind.Void;
        }

        public bool IsAuto()
        {
            return Kind == TypeKind.Auto;
        }

        public bool IsUndefined()
        {
            return Kind == TypeKind.Undefined;
        }

        public bool IsFloat()
        {
            return
                Kind is TypeKind.Float32
                or TypeKind.Float64
                or TypeKind.Float128;
        }

        public bool IsSignedInt()
        {
            return
                Kind is TypeKind.Int8
                or TypeKind.Int16
                or TypeKind.Int32
                or TypeKind.Int64;
        }

        public bool IsUnsignedInt()
        {
            return
                Kind is TypeKind.UInt8
                or TypeKind.UInt16
                or TypeKind.UInt32
                or TypeKind.UInt64;
        }
    }
}
