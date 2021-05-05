using Zap.Compilation;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST
{
  public class TypeAllocationNode : INode
    {
        public string NodeKind => "StructAllocation";
        public ZapType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
