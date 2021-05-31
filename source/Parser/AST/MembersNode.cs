using Mug.Compilation;
using Mug.Lexer;

namespace Mug.Parser.AST
{
    public class MemberNode : INode
    {
        public string NodeName => "Member";
        public INode Base { get; set; }
        public Token Member { get; set; }
        public ModulePosition Position { get; set; }
    }
}
