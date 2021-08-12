using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class MemberNode : INode
    {
        public string NodeName => "Member";
        public INode Base { get; set; }
        public Token Member { get; set; }
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            return $"{Base}.{Member}";
        }
    }
}
