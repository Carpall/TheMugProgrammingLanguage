using Mug.Compilation;
using Mug.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Parser.AST
{
  public class PostfixOperator : IStatement
    {
        public string NodeName => "PostfixOperator";
        public INode Expression { get; set; }
        public TokenKind Postfix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
