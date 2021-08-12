using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class EnumMemberNode : INode
    {
        public string NodeName => "EnumMember";
        public string Name { get; set; }
        public INode Value { get; set; }
        public ModulePosition Position { get; set; }

        

        public bool IsImplicitelyEnumerated() => Value is BadNode;

        public override string ToString()
        {
            return $"{Name}{(!IsImplicitelyEnumerated() ? $": {Value}" : null)},";
        }
    }
}
