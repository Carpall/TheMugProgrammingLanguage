using Mug.Compilation;
using Mug.Grammar;
using System.Collections.Immutable;

namespace Mug.Syntax.AST
{
    public class FieldNode : INode
    {
        public string NodeName => "Field";
        public Pragmas Pragmas { get; set; }
        public ImmutableArray<TokenKind> Modifiers { get; set; }
        public string Name { get; set; }
        public INode Type { get; set; }
        public ModulePosition Position { get; set; }

        public override string ToString()
        {
            return $"{Modifiers.Format()}{Name}: {Type}";
        }
    }
}
