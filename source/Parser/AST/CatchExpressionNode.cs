using Nylon.Compilation;
using Nylon.Models.Lexer;

namespace Nylon.Models.Parser.AST
{
  public struct CatchExpressionNode : INode
    {
        public string NodeKind => "Catch";
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public Token? OutError { get; set; }
        public ModulePosition Position { get; set; }
    }
}