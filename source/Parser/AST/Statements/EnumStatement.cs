using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public class EnumStatement : INode
    {
        public string NodeKind => "Enum";
        public Pragmas Pragmas { get; set; }
        public DataType BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
    }
}
