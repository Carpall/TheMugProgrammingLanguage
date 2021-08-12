using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Syntax.AST
{
    public class PrefixOperator : INode
    {
        public string NodeName => "PrefixOperator";
        public INode Expression { get; set; }
        public Token Prefix { get; set; }
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            return $"{Prefix.Kind.GetDescription()}{Expression}";
        }
    }
}
