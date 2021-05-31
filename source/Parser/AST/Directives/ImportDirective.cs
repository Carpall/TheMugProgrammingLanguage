using Mug.Compilation;

namespace Mug.Parser.AST.Directives
{
    public enum ImportMode
    {
        FromPackages,
        FromLocal
    }

    public class ImportDirective : INode
    {
        public string NodeName => "ImportDirective";
        public INode Member { get; set; }
        public ImportMode Mode { get; set; }
        public ModulePosition Position { get; set; }
    }
}
