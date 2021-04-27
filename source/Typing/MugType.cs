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
    }
}
