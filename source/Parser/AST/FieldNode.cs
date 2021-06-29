using Mug.Compilation;
using Mug.Grammar;


namespace Mug.Syntax.AST
{
    public class FieldNode : INode
    {
        public string NodeName => "Field";
        public Pragmas Pragmas { get; set; }
        public TokenKind Modifier { get; set; }
        public string Name { get; set; }
        public INode Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
