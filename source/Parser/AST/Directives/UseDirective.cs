using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST.Directives
{
    public class UseDirective : INode
    {
        public string NodeKind => "UseDirective";
        public Token Body { get; set; }
        public Token Alias { get; set; }
        public ModulePosition Position { get; set; }
    }
}
