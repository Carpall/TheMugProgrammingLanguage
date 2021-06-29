using Mug.Compilation;
using Mug.Grammar;

namespace Mug.Syntax.AST
{
    public class AssignmentNode : INode
    {
        public string NodeName => "Assignment";
        public Token Operator { get; set; }
        public INode Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
