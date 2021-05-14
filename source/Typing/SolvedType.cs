using Zap.Compilation;
using Zap.Models.Lexer;
using Zap.Models.Parser;
using Zap.Symbols;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Zap.TypeSystem
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

        public static SolvedType WithBase(TypeKind kind, ZapType baseElementType)
        {
            return new SolvedType { Kind = kind, Base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public static SolvedType EnumError(ZapType errorType, ZapType successType)
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

        public ZapType GetBaseElementType()
        {
            return (ZapType)Base;
        }

        public bool IsArray()
        {
            return Kind == TypeKind.Array;
        }

        public (ZapType ErrorType, ZapType SuccessType) GetEnumError()
        {
            return ((ZapType, ZapType))Base;
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
