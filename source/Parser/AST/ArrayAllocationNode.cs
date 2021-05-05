using Zap.Compilation;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST
{
    public class ArrayAllocationNode : INode
    {
        public string NodeKind => "ArrayAllocationNode";
        public ZapType Type { get; set; }
        public INode Size { get; set; }
        public bool SizeIsImplicit { get; set; }
        public List<INode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
