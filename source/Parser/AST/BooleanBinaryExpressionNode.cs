using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nylon.Models.Parser.AST
{
    public class BooleanBinaryExpressionNode : INode
    {
        public string NodeKind => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public TokenKind Operator { get; set; }
        public DataType IsInstructionType { get; set; }
        public ModulePosition Position { get; set; }
        public Token? IsInstructionAlias { get; set; }
    }
}
