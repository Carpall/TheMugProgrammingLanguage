using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.NodeKinds
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