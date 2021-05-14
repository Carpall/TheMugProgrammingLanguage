using Nylon.Compilation;
using Nylon.Models.Lexer;

namespace Nylon.Models.Parser.AST.Statements
{
    public class AssignmentStatement : IStatement
    {
        public string NodeKind => "Assignment";
        public TokenKind Operator { get; set; }
        public INode Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
