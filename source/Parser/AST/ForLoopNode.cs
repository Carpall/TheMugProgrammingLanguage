using Mug.Compilation;
using Mug.Grammar;

namespace Mug.Syntax.AST
{
    public struct ForLoopNode : INode
    {
        public string NodeName => "ForLoop";
        public Token Iterator { get; set; }
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }

        public override string ToString()
        {
            return $"for {Iterator} in {Expression} {Body}";
        }
    }
}
