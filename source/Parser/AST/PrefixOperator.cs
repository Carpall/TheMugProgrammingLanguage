using Nylon.Compilation;
using Nylon.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nylon.Models.Parser.AST
{
    public class PrefixOperator : IStatement
    {
        public string NodeKind => "PrefixOperator";
        public INode Expression { get; set; }
        public TokenKind Prefix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
