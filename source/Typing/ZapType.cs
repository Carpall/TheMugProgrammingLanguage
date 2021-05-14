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

        internal static ZapType Bool => Primitive(TypeKind.Bool);
        internal static ZapType Int32 => Primitive(TypeKind.Int32);
        internal static ZapType Void => Primitive(TypeKind.Void);
        internal static ZapType Auto => Primitive(TypeKind.Auto);

        internal static ZapType Primitive(TypeKind typekind)
        {
            return Solved(TypeSystem.SolvedType.Primitive(typekind));
        }

        internal static ZapType Unsolved(UnsolvedType unsolvedtype)
        {
            return new(unsolvedtype, default, false);
        }

        internal static ZapType Solved(SolvedType solvedtype)
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
