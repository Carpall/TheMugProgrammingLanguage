using Zap.Compilation;
using Zap.Models.Lexer;

namespace Zap.Models.Parser.AST.Statements
{
    public class LoopManagementStatement : INode
    {
        public string NodeKind => "LoopManagement";
        public Token Management { get; set; }
        public ModulePosition Position { get; set; }
    }
}
