using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.AST
{
    public class MemberNode : INode
    {
        public string NodeName => "Member";
        public INode Base { get; set; }
        public Token Member { get; set; }
        public ModulePosition Position { get; set; }
    }
}
