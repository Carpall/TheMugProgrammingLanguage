using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST
{
    public class NamespaceNode : INode
    {
        public string NodeKind => "Namespace";
        public NodeBuilder Members { get; set; }
        public Token Name { get; set; }
        public ModulePosition Position { get; set; }

        public NamespaceNode()
        {
            Members = new NodeBuilder();
        }
    }
}
