using Mug.Compilation;
using Mug.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Parser.AST
{
    public class PrefixOperator : INode
    {
        public string NodeName => "PrefixOperator";
        public INode Expression { get; set; }
        public Token Prefix { get; set; }
        public ModulePosition Position { get; set; }
    }
}
