using Mug.Compilation;
using Mug.Grammar;
using Newtonsoft.Json;

namespace Mug.Syntax.AST
{
    public class NamespaceNode : INode
    {
        public string NodeName => "Namespace";
        public NodeBuilder Members { get; set; }
        public Token Name { get; set; }
        public ModulePosition Position { get; set; }

        public NamespaceNode()
        {
            Members = new NodeBuilder();
        }
    }
}
