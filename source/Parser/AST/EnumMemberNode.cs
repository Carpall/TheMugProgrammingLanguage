using Mug.Compilation;
using Mug.Grammar;

namespace Mug.Syntax.AST
{
    public class EnumMemberNode : INode
    {
        public string NodeName => "EnumMember";
        public string Name { get; set; }
        public Token Value { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsNegative { get; set; }
    }
}
