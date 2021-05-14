using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public class VariantStatement : INode
    {
        public string NodeKind => "Variant";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<DataType> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
