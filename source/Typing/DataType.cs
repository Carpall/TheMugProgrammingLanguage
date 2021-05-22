using Mug.Compilation;
using Mug.Models.Lexer;
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
        internal static DataType Void => Primitive(TypeKind.Void);
        internal static DataType Auto => Primitive(TypeKind.Auto);

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
    }
}
