using Mug.Compilation;
using Mug.Models.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Models.Parser.AST
{
    public class PrefixOperator : IStatement
    {
        public string NodeName => "PrefixOperator";
        public INode Expression { get; set; }
        public Token Prefix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
