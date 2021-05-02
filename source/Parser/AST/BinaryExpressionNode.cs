using Mug.Compilation;
using Mug.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
    public class BinaryExpressionNode : INode
    {
        public string NodeKind => "BinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public TokenKind Operator { get; set; }
        public ModulePosition Position { get; set; }
    }
}
