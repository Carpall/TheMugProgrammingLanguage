using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
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

        public IType NodeType { get; set; } = null;

        public override string ToString()
        {
            return $"{Expression}{Postfix.Kind.GetDescription()}";
        }
    }
}
