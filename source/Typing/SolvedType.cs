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

        internal static SolvedType Create(TypeKind kind, object @base)
        {
            return new SolvedType { Kind = kind, Base = @base };
        }

        internal static SolvedType Enum(EnumStatement @enum)
        {
            return new SolvedType { Kind = TypeKind.Enum, Base = @enum};
        }

        public static SolvedType Struct(TypeStatement symbol)
        {
            return new SolvedType { Kind = TypeKind.CustomType, Base = symbol };
        }

        public static SolvedType WithBase(TypeKind kind, DataType baseElementType)
        {
            return new SolvedType { Kind = kind, Base = baseElementType };
        }

        public static SolvedType Primitive(TypeKind kind)
        {
            return new SolvedType { Kind = kind };
        }

        public TypeStatement GetStruct()
        {
            return Base as TypeStatement;
        }

        public bool IsStruct()
        {
            return Kind is TypeKind.CustomType or TypeKind.GenericDefinedType;
        }

        public (DataType Error, DataType Success) GetOption()
        {
            return ((DataType, DataType))Base;
        }

        public DataType GetBaseElementType()
        {
            return (DataType)Base;
        }

        public bool IsArray()
        {
            return Kind is TypeKind.Array;
        }

        public bool IsStringOrArray()
        {
            return IsArray() || IsString();
        }

        public override string ToString()
        {
            return UnsolvedType.TypeKindToString(
                Kind,
                Base is not null ?
                    Base is ISymbol symbol ?
                        symbol.ToString() :
                            Base.ToString() :
                            "");
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
                or TypeKind.CustomType
                or TypeKind.Array;
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

        public bool IsString()
        {
            return Kind is TypeKind.String;
        }

        public bool IsOption()
        {
            return Kind is TypeKind.Option;
        }

        public EnumStatement GetEnum()
        {
            return (EnumStatement)Base;
        }
    }
}
