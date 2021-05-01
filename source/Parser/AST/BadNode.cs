using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
{
    public class BadNode : INode
    {
        public string NodeKind => "BadNode";
        public ModulePosition Position { get; set; }
    }
}
