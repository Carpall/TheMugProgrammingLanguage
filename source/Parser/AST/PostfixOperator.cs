using Mug.Compilation;
using Mug.Grammar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Syntax.AST
{
    public class PostfixOperator : INode
    {
        public string NodeName => "PostfixOperator";
        public INode Expression { get; set; }
        public Token Postfix { get; set; }
        public ModulePosition Position { get; set; }

        public override string ToString()
        {
            return $"{Expression}{Postfix.Kind.GetDescription()}";
        }
    }
}
