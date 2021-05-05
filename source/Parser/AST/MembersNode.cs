using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST
{
    public class MemberNode : INode
    {
        public string NodeKind => "Member";
        public INode Base { get; set; }
        public Token Member { get; set; }
        public ModulePosition Position { get; set; }
    }
}
