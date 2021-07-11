using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class TryExpressionNode : INode
    {
        public string NodeName => "Try";
        public INode Expression { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public override string ToString()
        {
            return $"try {Expression}";
        }
    }
}