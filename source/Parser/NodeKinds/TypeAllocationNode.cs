using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds
{
  public class TypeAllocationNode : INode
    {
        public string NodeKind => "StructAllocation";
        public MugType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }

        public bool HasGenerics()
        {
            return Name.IsGeneric();
            // return Generics.Count != 0;
        }
    }
}
