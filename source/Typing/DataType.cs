using Mug.Compilation;
using Mug.Tokenizer;
using System;
using System.Collections.Generic;

namespace Mug.TypeSystem
{
    public class DataType
    {
        internal static Dictionary<TypeKind, TokenKind[]> TypesOperatorsImplementation = new()
        {
            [TypeKind.String] = new[] { TokenKind.Plus },
        };

        public UnsolvedType UnsolvedType { get; private set; }
        public SolvedType SolvedType { get; private set; }
        public bool IsSolved { get; private set; }
        public ModulePosition Position => UnsolvedType.Position;

        DataType(UnsolvedType unsolvedtype, SolvedType solvedtype, bool issolved)
        {
            UnsolvedType = unsolvedtype;
            SolvedType = solvedtype;
            IsSolved = issolved;
        }

        internal static DataType Bool => Primitive(TypeKind.Bool);
        internal static DataType Int32 => Primitive(TypeKind.Int32);
        internal static DataType Int64 => Primitive(TypeKind.Int64);
        internal static DataType Void => Primitive(TypeKind.Void);
        internal static DataType Auto => Primitive(TypeKind.Auto);
        internal static DataType String => Primitive(TypeKind.String);
        internal static DataType Char => Primitive(TypeKind.Char);
        internal static DataType Float32 => Primitive(TypeKind.Float32);
        internal static DataType Undefined => Primitive(TypeKind.Undefined);
        internal static DataType UInt64 => Primitive(TypeKind.UInt64);
        internal static DataType UInt8 => Primitive(TypeKind.UInt8);

        internal static DataType Option(DataType type)
        {
            return Solved(TypeSystem.SolvedType.WithBase(TypeKind.Option, type));
        }

        internal static DataType Pointer(DataType type)
        {
            return Solved(TypeSystem.SolvedType.WithBase(TypeKind.Pointer, type));
        }

        internal static DataType Primitive(TypeKind typekind)
        {
            return Solved(TypeSystem.SolvedType.Primitive(typekind));
        }

        internal static DataType Unsolved(UnsolvedType unsolvedtype)
        {
            return new(unsolvedtype, default, false);
        }

        internal static DataType Solved(SolvedType solvedtype)
        {
            return new(default, solvedtype, true);
        }

        internal void Solve(SolvedType solvedtype)
        {
            SolvedType = solvedtype;
            IsSolved = true;
        }

        public override string ToString()
        {
            return IsSolved ? SolvedType.ToString() : UnsolvedType.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is DataType type && SolvedType.Equals(type.SolvedType);
        }
    }
}
