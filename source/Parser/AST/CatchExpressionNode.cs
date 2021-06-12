using Mug.Compilation;
using Mug.Tokenizer;

namespace Mug.Parser.AST
{
  public struct CatchExpressionNode : INode
    {
        public string NodeName => "Catch";
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public Token? OutError { get; set; }
        public ModulePosition Position { get; set; }
    }
}