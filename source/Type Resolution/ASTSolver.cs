using Zap.Compilation;
using Zap.Models.Parser.AST;
using Zap.Symbols;
using Zap.TypeSystem;

namespace Zap.TypeResolution
{
    public class ASTSolver : ZapComponent
    {
        public ASTSolver(CompilationTower tower) : base(tower)
        {   
        }

        private SolvedType ResolveType(ZapType type)
        {
            if (type.IsSolved)
                return type.SolvedType;

            var unsolvedtype = type.UnsolvedType;

            return unsolvedtype.Kind switch
            {
                TypeKind.Array or
                TypeKind.Option or
                TypeKind.Pointer => SolvedType.WithBase(unsolvedtype.Kind, ZapType.Solved(ResolveType(unsolvedtype.BaseType as ZapType))),

                TypeKind.EnumError => SolvedType.EnumError(
                    ZapType.Solved(ResolveType(unsolvedtype.GetEnumError().ErrorType)),
                    ZapType.Solved(ResolveType(unsolvedtype.GetEnumError().SuccessType))),

                TypeKind.DefinedType => SolvedType.Struct(
                    Tower.Symbols.GetSymbol<StructSymbol>(unsolvedtype.BaseType as string, unsolvedtype.Position)),

                _ => SolvedType.Primitive(unsolvedtype.Kind),
            };
        }

        private void WalkTypes()
        {
            for (int i = 0; i < Tower.Types.Count; i++)
                if (!Tower.Types[i].IsSolved)
                    Tower.Types[i].Solve(ResolveType(Tower.Types[i]));
        }

        public NamespaceNode Solve()
        {
            var errorsnumber = Tower.Diagnostic.Count;
            Tower.TypeInstaller.Declare();

            WalkTypes();

            Tower.CheckDiagnostic(errorsnumber);
            return Tower.AST;
        }
    }
}
