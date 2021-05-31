using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Parser.AST
{
    public struct BadNode : INode
    {
        public string NodeName => "BadNode";
        public ModulePosition Position { get; set; }

        public BadNode(ModulePosition position)
        {
            Position = position;
        }
    }
}
