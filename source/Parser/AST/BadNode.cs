using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
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
