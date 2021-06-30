/*using Mug.Compilation;
using Mug.Grammar;

using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class EnumNode : INode
    {
        public string NodeName => "Enum";
        public Pragmas Pragmas { get; set; }
        public INode BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
*/