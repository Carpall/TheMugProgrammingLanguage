using Mug.Compilation;
using Mug.Tokenizer;

namespace Mug.Parser.AST.Statements
{
    public class AssignmentStatement : INode
    {
        public string NodeName => "Assignment";
        public Token Operator { get; set; }
        public INode Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
