using Mug.Compilation;
using Mug.Parser.AST;
using Mug.Parser.AST.Statements;
using Mug.Symbols;
using Mug.TypeSystem;

namespace Mug.TypeResolution
{
    public class ASTSolver : CompilerComponent
    {
        public ASTSolver(CompilationTower tower) : base(tower)
        {   
        }

        internal SolvedType ResolveType(DataType type)
        {
            if (type.IsSolved)
                return type.SolvedType;

            var unsolvedtype = type.UnsolvedType;

            return unsolvedtype.Kind switch
            {
                TypeKind.Array or
                TypeKind.Pointer => SolvePrimitiveWithBase(unsolvedtype),
                TypeKind.CustomType => SolveCustomType(unsolvedtype),
                TypeKind.Option => SolveOption(unsolvedtype),
                _ => SolvedType.Primitive(unsolvedtype.Kind),
            };
        }

        private static SolvedType SolveOption(UnsolvedType unsolvedtype)
        {
            return SolvedType.Create(TypeKind.Option, ((DataType, DataType))unsolvedtype.BaseType);
        }

        private SolvedType SolveCustomType(UnsolvedType unsolvedtype)
        {
            var symbol = Tower.Symbols.GetSymbol<ISymbol>(unsolvedtype.BaseType as string, unsolvedtype.Position, "type");

            return symbol switch
            {
                TypeStatement type => SolvedType.Struct(type),
                EnumStatement enumtype => SolvedType.Enum(enumtype),
                _ => error()
            };

            SolvedType error()
            {
                Tower.Report(unsolvedtype.Position, $"'{unsolvedtype}' is not a type");
                return SolvedType.Primitive(TypeKind.Undefined);
            }
        }

        private SolvedType SolvePrimitiveWithBase(UnsolvedType unsolvedtype)
        {
            return SolvedType.WithBase(unsolvedtype.Kind, DataType.Solved(ResolveType(unsolvedtype.BaseType as DataType)));
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
