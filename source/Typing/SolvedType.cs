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
        [JsonConverter(typeof(StringEnumConverter))]
        public TypeKind Kind;
        private object _base;
        public string PrettyBaseReppresentation => _base is not null ? _base.ToString() : "";

        public static SolvedType Struct(StructSymbol symbol)
        {
            return new SolvedType { Kind = TypeKind.DefinedType, _base = symbol };
        }

        public static SolvedType WithBase(TypeKind kind, SolvedType baseElementType)
        {
            return new SolvedType { Kind = kind, _base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public static SolvedType EnumError(SolvedType errorType, SolvedType successType)
        {
            return new SolvedType { Kind = TypeKind.EnumError, _base = (errorType, successType) };
        }

        public StructSymbol GetStruct()
        {
            Debug.Assert(IsStruct());
            return _base as StructSymbol;
        }

        public bool IsStruct()
        {
            return Kind == TypeKind.DefinedType;
        }

        public SolvedType GetArrayBaseElementType()
        {
            Debug.Assert(IsArray());
            return (SolvedType)_base;
        }

        public bool IsArray()
        {
            return Kind == TypeKind.Array;
        }

        public (SolvedType ErrorType, SolvedType SuccessType) GetEnumError()
        {
            return ((SolvedType, SolvedType))_base;
        }

        public override string ToString()
        {
            return UnsolvedType.TypeKindToString(
                Kind,
                _base is not null ? _base is ISymbol symbol ? symbol.Dump(false) : _base.ToString() : "",
                Kind == TypeKind.EnumError ? (GetEnumError().ToString(), GetEnumError().ToString()) : new());
        }
    }
}
