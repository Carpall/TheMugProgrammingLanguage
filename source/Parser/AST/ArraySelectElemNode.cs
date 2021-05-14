using Nylon.Compilation;

namespace Nylon.Models.Parser.AST
{
    public class ArraySelectElemNode : INode
    {
        public string NodeKind => "ArraySelectElemNode";
        public INode Left { get; set; }
        public INode IndexExpression { get; set; }
        public ModulePosition Position { get; set; }
    }
}
