using Mug.Compilation;
using Mug.Models.Parser.AST;
using Mug.Symbols;
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

        public NamespaceNode Solve()
        {
            Tower.TypeInstaller.Declare();

            Tower.CheckDiagnostic();
            return Tower.AST;
        }
    }
}
