using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.NodeKinds
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
