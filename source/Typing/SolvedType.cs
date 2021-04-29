using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Symbols;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mug.TypeSystem
{
    public struct SolvedType
    {
        public TypeKind Kind;
        internal object Base;
        public string BaseReppresentation => Base is not null ? Base.ToString() : "";

        public static SolvedType Struct(StructSymbol symbol)
        {
            return new SolvedType { Kind = TypeKind.DefinedType, Base = symbol };
        }

        public static SolvedType WithBase(TypeKind kind, SolvedType baseElementType)
        {
            return new SolvedType { Kind = kind, Base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public static SolvedType EnumError(SolvedType errorType, SolvedType successType)
        {
            return new SolvedType { Kind = TypeKind.EnumError, Base = (errorType, successType) };
        }

        public StructSymbol GetStruct()
        {
            Debug.Assert(IsStruct());
            return Base as StructSymbol;
        }

        public bool IsStruct()
        {
            return Kind == TypeKind.DefinedType;
        }

        public SolvedType GetArrayBaseElementType()
        {
            Debug.Assert(IsArray());
            return (SolvedType)Base;
        }

        public bool IsArray()
        {
            return Kind == TypeKind.Array;
        }

        public (SolvedType ErrorType, SolvedType SuccessType) GetEnumError()
        {
            return ((SolvedType, SolvedType))Base;
        }

        public override string ToString()
        {
            return UnsolvedType.TypeKindToString(
                Kind,
                Base is not null ? Base is ISymbol symbol ? symbol.Dump(false) : Base.ToString() : "",
                Kind == TypeKind.EnumError ? (GetEnumError().ToString(), GetEnumError().ToString()) : new());
        }

        public bool IsInt()
        {
            return
                Kind == TypeKind.Int32 ||
                Kind == TypeKind.Int64 ||
                Kind == TypeKind.UInt8 ||
                Kind == TypeKind.UInt32 ||
                Kind == TypeKind.UInt64;
        }

        public bool IsNewOperatorAllocable()
        {
            return
                Kind == TypeKind.GenericDefinedType ||
                Kind == TypeKind.DefinedType;
        }
    }
}
