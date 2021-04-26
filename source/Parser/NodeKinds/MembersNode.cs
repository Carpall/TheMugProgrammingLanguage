using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.NodeKinds
{
  public class MemberNode : INode
    {
        public string NodeKind => "Member";
        public INode Base { get; set; }
        public Token Member { get; set; }
        public ModulePosition Position { get; set; }
    }
}
