using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mug.Compilation;
using Mug.Syntax.AST;

namespace Mug.Backend
{
    public class Generator : CompilerComponent
    {
        public NamespaceNode AST { get; private set; }

        public MugIR IR { get; set; }

        public Generator(CompilationInstance tower) : base(tower)
        {
        }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
        }

        public MugIR Generate()
        {
            Reset();

            return IR;
        }

        private void Reset()
        {
            IR = new();
        }
    }
}
