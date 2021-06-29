using Mug.Compilation;
using Mug.Grammar;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Syntax.AST
{
    public class BooleanBinaryExpressionNode : INode
    {
        public string NodeName => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public Token Operator { get; set; }
        public ModulePosition Position { get; set; }
    }
}
