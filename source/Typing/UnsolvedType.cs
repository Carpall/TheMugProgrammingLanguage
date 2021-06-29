using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax;
using Mug.Syntax.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Mug.TypeSystem
{
    public struct UnsolvedType : INode
    {
        public string NodeName => "UnsolvedType";
        public TypeKind Kind { get; set; }
        public object BaseType { get; set; }
        public ModulePosition Position { get; set; }

        public static DataType Create(CompilationInstance tower, ModulePosition position, TypeKind type, object baseType = null)
        {
            var result = new UnsolvedType
            {
                Kind = type,
                BaseType = baseType,
                Position = position
            };

            tower.Types.Add(DataType.Unsolved(result));
            return tower.Types[^1];
        }

        public static UnsolvedType Create(TypeKind type, object baseType = null)
        {
            var result = new UnsolvedType
            {
                Kind = type,
                BaseType = baseType
            };

            return result;
        }

        /// <summary>
        /// converts a keyword token into a type
        /// </summary>
        public static DataType FromToken(CompilationInstance tower, Token token)
        {
            var type = GetTypeKindFromToken(token);
            return Create(tower, token.Position, type, token.Value);
        }

        public static TypeKind GetTypeKindFromToken(Token token)
        {
            return token.Value switch
            {
                "str" => TypeKind.String,
                "chr" => TypeKind.Char,
                "bool" => TypeKind.Bool,
                "i8" => TypeKind.Int8,
                "i16" => TypeKind.Int16,
                "i32" => TypeKind.Int32,
                "i64" => TypeKind.Int64,
                "f32" => TypeKind.Float32,
                "f64" => TypeKind.Float64,
                "f128" => TypeKind.Float128,
                "u8" => TypeKind.UInt8,
                "u16" => TypeKind.UInt16,
                "u32" => TypeKind.UInt32,
                "u64" => TypeKind.UInt64,
                "void" => TypeKind.Void,
                _ => TypeKind.CustomType,
            };
        }

        /// <summary>
        /// a short way of allocating with new operator
        /// </summary>
        public static DataType Automatic(CompilationInstance tower, ModulePosition position)
        {
            return Create(tower, position, TypeKind.Auto);
        }

        /// <summary>
        /// used for implicit type specification in var, const declarations
        /// </summary>
        public bool IsAutomatic()
        {
            return Kind is TypeKind.Auto;
        }

        public (UnsolvedType, List<UnsolvedType>) GetGenericStructure()
        {
            return ((UnsolvedType, List<UnsolvedType>))BaseType;
        }

        public bool IsGeneric()
        {
            return BaseType is (UnsolvedType, List<UnsolvedType>);
        }

        /// <summary>
        /// returns a string reppresentation of the type
        /// </summary>
        public override string ToString()
        {
            return TypeKindToString(
                Kind,
                BaseType is not null ?
                    BaseType.ToString() :
                    "");
        }

        public static string TypeKindToString(TypeKind kind, string basetype)
        {
            return kind switch
            {
                TypeKind.Auto => "auto",
                TypeKind.Array => $"[{basetype}]",
                TypeKind.Bool => "bool",
                TypeKind.Char => "chr",
                TypeKind.CustomType => basetype,
                // TypeKind.GenericDefinedType => GetGenericStructure().Item1.ToString(),
                TypeKind.Int8 => "i8",
                TypeKind.Int16 => "i16",
                TypeKind.Int32 => "i32",
                TypeKind.Int64 => "i64",
                TypeKind.Float32 => "f32",
                TypeKind.Float64 => "f64",
                TypeKind.Float128 => "f128",
                TypeKind.UInt8 => "u8",
                TypeKind.UInt16 => "u16",
                TypeKind.UInt32 => "u32",
                TypeKind.UInt64 => "u64",
                TypeKind.Pointer => $"*{basetype}",
                TypeKind.String => "str",
                TypeKind.Void => "void",
                TypeKind.Undefined => "undefined",
                TypeKind.Option => $"option[{basetype}]",
                TypeKind.Enum => basetype,
                TypeKind.EmptyEnum => "anyerror"
            };
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

        public (DataType ErrorType, DataType SuccessType) GetEnumError()
        {
            return ((DataType, DataType))BaseType;
        }

        public override bool Equals(object obj)
        {
            if (obj is not UnsolvedType type || type.Kind != Kind)
                return false;

            if (BaseType is not null && type.BaseType is not null)
                return BaseType.Equals(type.BaseType);

            return true;
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
