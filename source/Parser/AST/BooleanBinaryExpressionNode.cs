using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
    public class BooleanBinaryExpressionNode : INode
    {
        public string NodeKind => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        public TokenKind Operator { get; set; }
        public MugType IsInstructionType { get; set; }
        public ModulePosition Position { get; set; }
        public Token? IsInstructionAlias { get; set; }
    }
}
