using Zap.Compilation;
using Zap.TypeSystem;

namespace Zap.Models.Parser.AST
{
    public class CastExpressionNode : INode
    {
        public string NodeKind => "Cast";
        public INode Expression { get; set; }
        public ZapType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}