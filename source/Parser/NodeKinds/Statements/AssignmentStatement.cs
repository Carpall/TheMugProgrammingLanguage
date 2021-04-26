using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.NodeKinds.Statements
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
