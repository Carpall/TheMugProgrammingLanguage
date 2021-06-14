using Mug.Compilation;
using Mug.Tokenizer;

namespace Mug.Parser.AST.Statements
{
    public class LoopManagementStatement : INode
    {
        public string NodeName => "LoopManagement";
        public TokenKind Kind { get; set; }
        public ModulePosition Position { get; set; }
    }
}
