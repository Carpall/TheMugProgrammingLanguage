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
        public TypeKind Kind { get; set; }
        [JsonIgnore]
        public object Base { get; set; }
        public string BaseReppresentation => Base is not null ? Base.ToString() : "";

        public static SolvedType Struct(StructSymbol symbol)
        {
            return new SolvedType { Kind = TypeKind.DefinedType, Base = symbol };
        }

        public static SolvedType WithBase(TypeKind kind, MugType baseElementType)
        {
            return new SolvedType { Kind = kind, Base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public static SolvedType EnumError(MugType errorType, MugType successType)
        {
            return new SolvedType { Kind = TypeKind.EnumError, Base = (errorType, successType) };
        }

        public StructSymbol GetStruct()
        {
            return Base as StructSymbol;
        }

        public bool IsStruct()
        {
            return Kind == TypeKind.DefinedType || Kind == TypeKind.GenericDefinedType;
        }

        public MugType GetBaseElementType()
        {
            return (MugType)Base;
        }

        public bool IsArray()
        {
            return Kind == TypeKind.Array;
        }

        public (MugType ErrorType, MugType SuccessType) GetEnumError()
        {
            return ((MugType, MugType))Base;
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
    }
}
