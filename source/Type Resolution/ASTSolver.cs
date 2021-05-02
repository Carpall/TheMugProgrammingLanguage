using LLVMSharp;
using Mug.Compilation;
using Mug.Models.Parser;
using Mug.Models.Parser.AST;
using Mug.Models.Parser.AST.Statements;
using Mug.Symbols;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.TypeResolution
{
    public class ASTSolver : MugComponent
    {
        public ASTSolver(CompilationTower tower) : base(tower)
        {   
        }

        private SolvedType ResolveType(MugType type)
        {
            if (!type.UnsolvedType.HasValue)
                return type.SolvedType.Value;

            var unsolvedtype = type.UnsolvedType.Value;

            return unsolvedtype.Kind switch
            {
                TypeKind.Array or
                TypeKind.Pointer => SolvedType.WithBase(unsolvedtype.Kind, MugType.Solved(ResolveType(unsolvedtype.BaseType as MugType))),

                TypeKind.EnumError => SolvedType.EnumError(
                    MugType.Solved(ResolveType(unsolvedtype.GetEnumError().ErrorType)),
                    MugType.Solved(ResolveType(unsolvedtype.GetEnumError().SuccessType))),

                TypeKind.DefinedType => SolvedType.Struct(
                    Tower.Symbols.GetSymbol<StructSymbol>(unsolvedtype.BaseType as string, unsolvedtype.Position)),

                _ => SolvedType.Primitive(unsolvedtype.Kind),
            };
        }

        private void WalkTypes()
        {
            for (int i = 0; i < Tower.Types.Count; i++)
                if (Tower.Types[i].UnsolvedType.HasValue)
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
