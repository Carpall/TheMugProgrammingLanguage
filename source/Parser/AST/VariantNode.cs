using Mug.Compilation;
using Mug.Grammar;

using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class VariantNode : INode
    {
        public string NodeName => "Variant";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<INode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
