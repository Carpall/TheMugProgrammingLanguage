using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Mug.TypeSystem
{
    public class UnsolvedType : INode, IType
    {
        public string NodeKind => "UnsolvedType";
        [JsonConverter(typeof(StringEnumConverter))]
        public TypeKind Kind { get; set; }
        public object BaseType { get; set; }
        public ModulePosition Position { get; set; }

        /// <summary>
        /// basetype is used when kind is a non primitive type, a pointer or an array
        /// </summary>
        public UnsolvedType(ModulePosition position, TypeKind type, object baseType = null)
        {
            Kind = type;
            BaseType = baseType;
            Position = position;
        }

        /// <summary>
        /// converts a keyword token into a type
        /// </summary>
        public static UnsolvedType FromToken(Token t, bool isInEnum = false)
        {
            return new UnsolvedType(t.Position, t.Value switch
            {
                "str" => TypeKind.String,
                "chr" => TypeKind.Char,
                "bool" => TypeKind.Bool,
                "i32" => TypeKind.Int32,
                "i64" => TypeKind.Int64,
                "f32" => TypeKind.Float32,
                "f64" => TypeKind.Float64,
                "f128" => TypeKind.Float128,
                "u8" => TypeKind.UInt8,
                "void" => TypeKind.Void,
                "unknown" => TypeKind.Unknown,
                _ => isInEnum && t.Value == "err" ? TypeKind.Err : TypeKind.DefinedType,
            }, t.Value);
        }

        /// <summary>
        /// a short way of allocating with new operator
        /// </summary>
        public static UnsolvedType Automatic(ModulePosition position)
        {
            return new UnsolvedType(position, TypeKind.Auto);
        }

        /// <summary>
        /// used for implicit type specification in var, const declarations
        /// </summary>
        public bool IsAutomatic()
        {
            return Kind == TypeKind.Auto;
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
            return Kind switch
            {
                TypeKind.Auto => "auto",
                TypeKind.Array => $"[{BaseType}]",
                TypeKind.Bool => "bool",
                TypeKind.Char => "chr",
                TypeKind.DefinedType => BaseType.ToString(),
                TypeKind.GenericDefinedType => GetGenericStructure().Item1.ToString(),
                TypeKind.Int32 => "i32",
                TypeKind.Int64 => "i64",
                TypeKind.Float32 => "f32",
                TypeKind.Float64 => "f64",
                TypeKind.Float128 => "f128",
                TypeKind.UInt8 => "u8",
                TypeKind.UInt32 => "u32",
                TypeKind.UInt64 => "u64",
                TypeKind.Unknown => "unknown",
                TypeKind.Pointer => $"*{BaseType}",
                TypeKind.String => "str",
                TypeKind.Reference => $"&{BaseType}",
                TypeKind.Void => "void",
                TypeKind.Err => "err",
                TypeKind.EnumError => $"{GetEnumError().Item1}!{GetEnumError().Item2}",
            };
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

        private (UnsolvedType, UnsolvedType) GetEnumError()
        {
            return ((UnsolvedType, UnsolvedType))BaseType;
        }

        public override bool Equals(object obj)
        {
            if (obj is not UnsolvedType type || type.Kind != Kind)
                return false;

            if (BaseType is not null && type.BaseType is not null)
                return BaseType.Equals(type.BaseType);

            return true;
        }
    }
}
