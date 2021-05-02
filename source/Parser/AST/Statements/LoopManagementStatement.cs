using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.AST.Statements
{
    public class LoopManagementStatement : INode
    {
        public string NodeKind => "LoopManagement";
        public Token Management { get; set; }
        public ModulePosition Position { get; set; }
    }
}
