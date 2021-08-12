using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class LoopManagementNode : INode
    {
        public string NodeName => "LoopManagement";
        public TokenKind Kind { get; set; }
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            return Kind.GetDescription();
        }
    }
}
