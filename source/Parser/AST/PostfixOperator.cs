using Mug.Compilation;
using Mug.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
  public class PostfixOperator : IStatement
    {
        public string NodeName => "PostfixOperator";
        public INode Expression { get; set; }
        public TokenKind Postfix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
