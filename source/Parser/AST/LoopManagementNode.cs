using Mug.Compilation;
using Mug.Grammar;

namespace Mug.Syntax.AST
{
    public class LoopManagementNode : INode
    {
        public string NodeName => "LoopManagement";
        public TokenKind Kind { get; set; }
        public ModulePosition Position { get; set; }
    }
}
