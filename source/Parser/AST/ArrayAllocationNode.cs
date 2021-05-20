using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
    public class ArrayAllocationNode : INode
    {
        public string NodeKind => "ArrayAllocationNode";
        public DataType Type { get; set; }
        public INode Size { get; set; }
        public bool SizeIsImplicit { get; set; }
        public List<INode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
