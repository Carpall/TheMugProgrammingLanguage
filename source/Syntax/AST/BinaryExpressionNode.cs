using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Syntax.AST
{
    public class BinaryExpressionNode : INode
    {
        public string NodeName => "BinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public Token Operator { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public override string ToString()
        {
            return $"({Left} {Operator.Kind.GetDescription()} {Right})";
        }
    }
}
