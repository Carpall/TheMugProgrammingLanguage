using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
  public class TypeAllocationNode : INode
    {
        public string NodeKind => "StructAllocation";
        public UnsolvedType Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }

        public bool HasGenerics()
        {
            return Name.IsGeneric();
            // return Generics.Count != 0;
        }
    }
}
