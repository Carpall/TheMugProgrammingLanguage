using Mug.Compilation;
using Mug.Tokenizer;

namespace Mug.Parser.AST
{
    public struct TryExpressionNode : INode
    {
        public string NodeName => "Try";
        public INode Expression { get; set; }
        public ModulePosition Position { get; set; }
    }
}