using System.Collections.Generic;
using System.Collections.Immutable;
using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using Newtonsoft.Json;

namespace Mug.Syntax.AST
{
    public class NamespaceNode : INode
    {
        public string NodeName => "Namespace";
        public List<VariableNode> Members { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public NamespaceNode()
        {
            Members = new();
        }

        public override string ToString()
        {
            return string.Join('\n', Members)/*(this as INode).Dump()*/;
        }
    }
}
