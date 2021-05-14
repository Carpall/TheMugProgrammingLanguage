using Zap.Compilation;
using Zap.Models.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.TypeSystem
{
    public class ZapType
    {
        public UnsolvedType UnsolvedType { get; private set; }
        public SolvedType SolvedType { get; private set; }
        public bool IsSolved { get; private set; }
        public ModulePosition Position => UnsolvedType.Position;

        ZapType(UnsolvedType unsolvedtype, SolvedType solvedtype, bool issolved)
        {
            UnsolvedType = unsolvedtype;
            SolvedType = solvedtype;
            IsSolved = issolved;
        }

        internal static ZapType Bool => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Bool));
        internal static ZapType Int32 => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Int32));
        internal static ZapType Void => Solved(TypeSystem.SolvedType.Primitive(TypeKind.Void));

        public static ZapType Unsolved(UnsolvedType unsolvedtype)
        {
            return new(unsolvedtype, default, false);
        }

        public static ZapType Solved(SolvedType solvedtype)
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
