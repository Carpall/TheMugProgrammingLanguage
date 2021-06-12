using Mug.Compilation;
using Mug.Tokenizer;

namespace Mug.Parser.AST
{
    public class EnumMemberNode : INode
    {
        public string NodeName => "EnumMember";
        public string Name { get; set; }
        public Token Value { get; set; }
        public ModulePosition Position { get; set; }
    }
}
