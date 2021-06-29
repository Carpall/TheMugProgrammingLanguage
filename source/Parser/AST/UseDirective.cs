using Mug.Compilation;
using Mug.Grammar;

namespace Mug.Syntax.AST.Directives
{
    public class UseDirective : INode
    {
        public string NodeName => "UseDirective";
        public Token Body { get; set; }
        public Token Alias { get; set; }
        public ModulePosition Position { get; set; }
    }
}
