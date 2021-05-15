using Nylon.Compilation;
using Nylon.TypeSystem;

namespace Nylon.Models.Parser.AST
{
    public struct BadNode : INode
    {
        public string NodeKind => "BadNode";
        public ModulePosition Position { get; set; }

        public BadNode(ModulePosition position)
        {
            Position = position;
        }
    }
}
