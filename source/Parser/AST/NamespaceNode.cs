using System.Collections.Generic;
using System.Collections.Immutable;
using Mug.Compilation;
using Mug.Grammar;
using Newtonsoft.Json;

namespace Mug.Syntax.AST
{
    public class NamespaceNode : INode
    {
        public string NodeName => "Namespace";
        public List<VariableNode> Members { get; set; }
        public ModulePosition Position { get; set; }

        public NamespaceNode()
        {
            Members = new();
        }

        public override string ToString()
        {
            return (this as INode).Dump();
        }
    }
}
