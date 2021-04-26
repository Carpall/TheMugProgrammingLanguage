using Mug.Compilation;
using Mug.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
  public class PrefixOperator : IStatement
    {
        public string NodeKind => "PrefixOperator";
        public INode Expression { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenKind Prefix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
