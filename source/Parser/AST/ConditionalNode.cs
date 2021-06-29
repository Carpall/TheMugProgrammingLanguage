using Mug.Compilation;
using Mug.Grammar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    }
}
