using Zap.Compilation;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zap.Models.Parser.AST
{
    public class BooleanBinaryExpressionNode : INode
    {
        public string NodeKind => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public TokenKind Operator { get; set; }
        public ZapType IsInstructionType { get; set; }
        public ModulePosition Position { get; set; }
        public Token? IsInstructionAlias { get; set; }
    }
}
