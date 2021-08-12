using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class EnumNode : INode
    {
        public string NodeName => "Enum";
        public INode BaseType { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }

        

        public bool IsAuto() => BaseType is BadNode;

        public override string ToString()
        {
            var result = new BlockNode();
            result.Statements.AddRange(Body);

            return $"(enum {(IsAuto() ? null : $"{BaseType} ")}{result})";
        }
    }
}