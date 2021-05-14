using Nylon.Compilation;
using Nylon.Models.Lexer;

namespace Nylon.Models.Parser.AST
{
    public class EnumMemberNode : INode
    {
        public string NodeKind => "EnumMember";
        public string Name { get; set; }
        public Token Value { get; set; }
        public ModulePosition Position { get; set; }
    }
}
