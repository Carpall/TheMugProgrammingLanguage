using Nylon.Compilation;
using Nylon.Models.Lexer;

namespace Nylon.Models.Parser.AST.Statements
{
    public class LoopManagementStatement : INode
    {
        public string NodeKind => "LoopManagement";
        public Token Management { get; set; }
        public ModulePosition Position { get; set; }
    }
}
