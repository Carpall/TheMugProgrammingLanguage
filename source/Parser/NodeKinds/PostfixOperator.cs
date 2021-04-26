using Mug.Compilation;
using Mug.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.NodeKinds
{
  public class PostfixOperator : IStatement
    {
        public string NodeKind => "PostfixOperator";
        public INode Expression { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenKind Postfix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
