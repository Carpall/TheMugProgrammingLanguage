using Mug.Compilation;
using Mug.Syntax.AST;

namespace Mug.Semantic
{
    class Evaluator : CompilerComponent
    {
        private NamespaceNode AST { get; set; }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
        }

        public Evaluator(CompilationInstance tower) : base(tower)
        {
        }

        public NamespaceNode Check()
        {   
            return AST;
        }
    }
}