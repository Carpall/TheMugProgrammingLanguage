using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
  public class TypeAllocationNode : INode
    {
        public string NodeKind => "StructAllocation";
        public MugType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
