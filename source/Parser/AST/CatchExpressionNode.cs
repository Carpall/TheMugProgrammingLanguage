using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST
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