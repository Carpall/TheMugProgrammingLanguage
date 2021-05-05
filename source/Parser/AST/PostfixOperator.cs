using Zap.Compilation;
using Zap.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zap.Models.Parser.AST
{
  public class PostfixOperator : IStatement
    {
        public string NodeKind => "PostfixOperator";
        public INode Expression { get; set; }
        public TokenKind Postfix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
