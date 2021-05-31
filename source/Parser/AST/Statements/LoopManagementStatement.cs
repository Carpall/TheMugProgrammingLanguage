using Mug.Compilation;
using Mug.Lexer;

namespace Mug.Parser.AST.Statements
{
    public class LoopManagementStatement : INode
    {
        public string NodeName => "LoopManagement";
        public Token Management { get; set; }
        public ModulePosition Position { get; set; }
    }
}
