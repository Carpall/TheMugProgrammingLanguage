using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST.Statements
{
    public class VariantStatement : INode
    {
        public string NodeKind => "Variant";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<MugType> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
