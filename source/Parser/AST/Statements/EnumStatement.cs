using Mug.Compilation;
using Mug.Lexer;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Parser.AST.Statements
{
    public class EnumStatement : INode
    {
        public string NodeName => "Enum";
        public Pragmas Pragmas { get; set; }
        public DataType BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
    }
}
