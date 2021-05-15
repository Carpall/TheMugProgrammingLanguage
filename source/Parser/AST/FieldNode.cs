using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;

namespace Nylon.Models.Parser.AST
{
    public class FieldNode : INode
    {
        public string NodeKind => "Field";
        public Pragmas Pragmas { get; set; }
        public TokenKind Modifier { get; set; }
        public string Name { get; set; }
        public DataType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
