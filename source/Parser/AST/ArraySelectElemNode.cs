using Mug.Compilation;

namespace Mug.Syntax.AST
{
    public class ArraySelectElemNode : INode
    {
        public string NodeName => "ArraySelectElemNode";
        public INode Left { get; set; }
        public INode IndexExpression { get; set; }
        public ModulePosition Position { get; set; }
    }
}
