using Mug.Compilation;
using Mug.Grammar;
using Mug.TypeSystem;
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
        public DataType IsInstructionType { get; set; }
        public ModulePosition Position { get; set; }
        public Token? IsInstructionAlias { get; set; }
    }
}
