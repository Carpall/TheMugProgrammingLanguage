using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
    public class TypeAllocationNode : INode
    {
        public string NodeName => "StructAllocation";
        public DataType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public bool IsAuto => Name is null;
    }
}
