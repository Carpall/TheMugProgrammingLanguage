using Mug.Compilation;
using Mug.Typing;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class CallNode : INode
    {
        public string NodeName => "Call";
        public NodeBuilder Parameters { get; set; } = new();
        public INode Name { get; set; }
        public bool IsBuiltIn { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public override string ToString()
        {
            return $"{Name}{(IsBuiltIn ? "!" : null)}({string.Join(", ", Parameters)})";
        }
    }
}
