using Mug.Compilation;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public struct BadNode : INode
    {
        public string NodeName => "BadNode";
        public ModulePosition Position { get; set; }

        

        public BadNode(ModulePosition position)
        {
            Position = position;
            
        }

        public override string ToString()
        {
            return "BadNode";
        }
    }
}
