using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST
{
    public struct TryExpressionNode : INode
    {
        public string NodeKind => "Try";
        public INode Expression { get; set; }
        public ModulePosition Position { get; set; }
    }
}