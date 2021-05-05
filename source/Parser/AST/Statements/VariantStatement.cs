using Zap.Compilation;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST.Statements
{
    public class VariantStatement : INode
    {
        public string NodeKind => "Variant";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<ZapType> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
