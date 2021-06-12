using Mug.Compilation;
using Mug.Tokenizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Parser.AST
{
    public class BinaryExpressionNode : INode
    {
        public string NodeName => "BinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public Token Operator { get; set; }
        public ModulePosition Position { get; set; }
    }
}
