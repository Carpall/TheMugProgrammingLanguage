using Mug.Compilation;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class CastExpressionNode : INode
    {
        public string NodeName => "Cast";
        public INode Expression { get; set; }
        public INode Type { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public override string ToString()
        {
            return $"{Expression} as {Type}";
        }
    }
}