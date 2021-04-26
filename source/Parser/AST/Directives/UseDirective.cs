using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.AST.Directives
{
  public class UseDirective : INode
    {
        public string NodeKind => "UseDirective";
        public INode Body { get; set; }
        public Token Alias { get; set; }
        public ModulePosition Position { get; set; }
    }
}
