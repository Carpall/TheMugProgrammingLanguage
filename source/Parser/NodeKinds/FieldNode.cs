using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.NodeKinds
{
  public class FieldNode : INode
    {
        public string NodeKind => "Field";
        public string Name { get; set; }
        public MugType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
