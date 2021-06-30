using Mug.Compilation;
using Mug.Grammar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace Mug.Syntax.AST
{
    public class ConditionalNode : INode
    {
        public string NodeName => "Condition";
        public TokenKind Kind { get; set; }
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }
        public ConditionalNode ElseNode { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();
            var node = this;

            while (node is not null)
                result.Append($"{Kind.GetDescription()} {Expression} {Body}");

            return result.ToString();
        }
    }
}
