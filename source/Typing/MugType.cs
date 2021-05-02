using Mug.Compilation;
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
        public UnsolvedType UnsolvedType { get; private set; }
        public SolvedType SolvedType { get; private set; }
        public bool IsSolved { get; private set; }
        public ModulePosition Position => UnsolvedType.Position;

        MugType(UnsolvedType unsolvedtype, SolvedType solvedtype, bool issolved)
        {
            UnsolvedType = unsolvedtype;
            SolvedType = solvedtype;
            IsSolved = issolved;
        }

        internal static MugType Int32 => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Int32));
        internal static MugType Void => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Void));

        public static MugType Unsolved(UnsolvedType unsolvedtype)
        {
            return new(unsolvedtype, default, false);
        }

        public static MugType Solved(SolvedType solvedtype)
        {
            return new(default, solvedtype, true);
        }

        public void Solve(SolvedType solvedtype)
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
