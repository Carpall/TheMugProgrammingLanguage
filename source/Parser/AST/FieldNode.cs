using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
{
    public class FieldNode : INode
    {
        public string NodeName => "Field";
        public Pragmas Pragmas { get; set; }
        public TokenKind Modifier { get; set; }
        public string Name { get; set; }
        public DataType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
