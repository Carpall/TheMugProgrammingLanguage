using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.AST
{
  public class EnumMemberNode : INode
    {
        public string NodeKind => "EnumMember";
        public string Name { get; set; }
        public Token Value { get; set; }
        public ModulePosition Position { get; set; }
    }
}
