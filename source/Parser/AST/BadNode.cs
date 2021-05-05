using Zap.Compilation;
using Zap.TypeSystem;

namespace Zap.Models.Parser.AST
{
    public class BadNode : INode
    {
        public string NodeKind => "BadNode";
        public ModulePosition Position { get; set; }
    }
}
