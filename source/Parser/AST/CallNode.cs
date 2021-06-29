using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class CallNode : INode
    {
        public string NodeName => "Call";
        public NodeBuilder Parameters { get; set; } = new();
        public INode Name { get; set; }
        public List<DataType> Generics { get; set; } = new();
        public bool IsBuiltIn { get; set; }
        public ModulePosition Position { get; set; }
    }
}
