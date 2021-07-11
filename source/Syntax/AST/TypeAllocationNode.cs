using Mug.Compilation;
using Mug.Typing;
using System.Collections.Generic;
using System.Text;

namespace Mug.Syntax.AST
{
    public class TypeAllocationNode : INode
    {
        public string NodeName => "StructAllocation";
        public INode Name { get; set; }
        public List<FieldAssignmentNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public bool IsAuto() => Name is BadNode;

        public override string ToString()
        {
            BlockNode.Indent += "  ";
            var body = new StringBuilder($"{{\n");
            foreach (var field in Body)
                body.AppendLine($"{BlockNode.Indent}{field},");

            BlockNode.Indent = BlockNode.Indent[..^2];
            body.Append($"{BlockNode.Indent}}}");
            return $"new {(IsAuto() ? null : $"{Name} ")}{body}";
        }
    }
}
