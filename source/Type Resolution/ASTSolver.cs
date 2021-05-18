using Nylon.Compilation;
using Nylon.Models.Parser.AST;
using Nylon.Models.Parser.AST.Statements;
using Nylon.Symbols;
using Nylon.TypeSystem;

namespace Nylon.TypeResolution
{
    public class ASTSolver : CompilerComponent
    {
        public ASTSolver(CompilationTower tower) : base(tower)
        {   
        }

        private SolvedType ResolveType(DataType type)
        {
            if (type.IsSolved)
                return type.SolvedType;

            var unsolvedtype = type.UnsolvedType;

            return unsolvedtype.Kind switch
            {
                TypeKind.Array or
                TypeKind.Option or
                TypeKind.Pointer => SolvePrimitiveWithBase(unsolvedtype),
                TypeKind.EnumError => SolveEnumError(unsolvedtype),
                TypeKind.DefinedType => SolveStruct(unsolvedtype),
                _ => SolvedType.Primitive(unsolvedtype.Kind),
            };
        }

        private SolvedType SolveStruct(UnsolvedType unsolvedtype)
        {
            return SolvedType.Struct(
                Tower.Symbols.GetSymbol<TypeStatement>(unsolvedtype.BaseType as string, unsolvedtype.Position, "type"));
        }

        private SolvedType SolvePrimitiveWithBase(UnsolvedType unsolvedtype)
        {
            return SolvedType.WithBase(unsolvedtype.Kind, DataType.Solved(ResolveType(unsolvedtype.BaseType as DataType)));
        }

        private SolvedType SolveEnumError(UnsolvedType unsolvedtype)
        {
            return SolvedType.EnumError(
                DataType.Solved(ResolveType(unsolvedtype.GetEnumError().ErrorType)),
                DataType.Solved(ResolveType(unsolvedtype.GetEnumError().SuccessType)));
        }

        private void WalkTypes()
        {
            for (var i = 0; i < Tower.Types.Count; i++)
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
