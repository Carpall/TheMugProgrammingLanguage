using Nylon.Compilation;
using Nylon.TypeSystem;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST
{
  public class TypeAllocationNode : INode
    {
        public string NodeKind => "StructAllocation";
        public DataType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
