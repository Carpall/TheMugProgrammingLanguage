using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Symbols;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mug.Models.Parser.AST.Statements;

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
                Kind == TypeKind.Int8 ||
                Kind == TypeKind.Int16 ||
                Kind == TypeKind.Int32 ||
                Kind == TypeKind.Int64 ||
                Kind == TypeKind.UInt8 ||
                Kind == TypeKind.UInt16 ||
                Kind == TypeKind.UInt32 ||
                Kind == TypeKind.UInt64;
        }

        public bool IsNewOperatorAllocable()
        {
            return
                Kind == TypeKind.GenericDefinedType ||
                Kind == TypeKind.DefinedType;
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
    }
}
