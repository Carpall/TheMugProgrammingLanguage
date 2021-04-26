using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST.Statements
{
    public class EnumStatement : INode
    {
        public string NodeKind => "Enum";
        public Pragmas Pragmas { get; set; }
        public UnsolvedType BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
    }
}
