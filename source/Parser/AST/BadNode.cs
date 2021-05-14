using Nylon.Compilation;
using Nylon.TypeSystem;

namespace Nylon.Models.Parser.AST
{
    public class BadNode : INode
    {
        public string NodeKind => "BadNode";
        public ModulePosition Position { get; set; }
    }
}
