using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
{
  public class FieldNode : INode
    {
        public string NodeKind => "Field";
        public string Name { get; set; }
        public IType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
