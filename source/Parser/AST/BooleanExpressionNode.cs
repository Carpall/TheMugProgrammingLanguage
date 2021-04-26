using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
  public class BooleanExpressionNode : INode
    {
        public string NodeKind => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenKind Operator { get; set; }
        public IType IsInstructionType { get; set; }
        public ModulePosition Position { get; set; }
        public Token? IsInstructionAlias { get; set; }
    }
}
