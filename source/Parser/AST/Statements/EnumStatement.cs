using Zap.Compilation;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST.Statements
{
    public class EnumStatement : INode
    {
        public string NodeKind => "Enum";
        public Pragmas Pragmas { get; set; }
        public ZapType BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
    }
}
