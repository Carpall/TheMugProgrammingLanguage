using Mug.Compilation;

namespace Mug.Syntax.AST
{
    public struct ForLoopNode : INode
    {
        public string NodeName => "ForLoop";
        public INode LeftExpression { get; set; }
        public INode ConditionExpression { get; set; }
        public INode RightExpression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
