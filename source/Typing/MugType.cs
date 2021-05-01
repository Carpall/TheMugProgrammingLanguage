using Mug.Models.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.TypeSystem
{
    public class MugType
    {
        public UnsolvedType? UnsolvedType { get; private set; }
        public SolvedType? SolvedType { get; private set; }

        MugType(UnsolvedType? unsolvedtype, SolvedType? solvedtype)
        {
            UnsolvedType = unsolvedtype;
            SolvedType = solvedtype;
        }

        internal static MugType Int32 => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Int32));
        internal static MugType Void => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Void));

        public static MugType Unsolved(UnsolvedType unsolvedtype)
        {
            return new(unsolvedtype, null);
        }

        public static MugType Solved(SolvedType solvedtype)
        {
            return new(null, solvedtype);
        }

        public void Solve(SolvedType solvedtype)
        {
            SolvedType = solvedtype;
            UnsolvedType = null;
        }

        public override string ToString()
        {
            return UnsolvedType.HasValue ? UnsolvedType.Value.ToString() : SolvedType.Value.ToString();
        }
    }
}
