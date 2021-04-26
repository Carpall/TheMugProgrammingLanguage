using Mug.Compilation;

namespace Mug.Models.Parser.NodeKinds.Directives
{
  public enum ImportMode
    {
        FromPackages,
        FromLocal
    }
    public class ImportDirective : INode
    {
        public string NodeKind => "ImportDirective";
        public INode Member { get; set; }
        public ImportMode Mode { get; set; }
        public ModulePosition Position { get; set; }
    }
}
