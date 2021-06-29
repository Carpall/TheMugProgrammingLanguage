using Mug.Compilation;

using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class TypeAllocationNode : INode
    {
        public string NodeName => "StructAllocation";
        public INode Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public bool IsAuto => Name is null;
    }
}
