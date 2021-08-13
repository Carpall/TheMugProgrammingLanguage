using Mug.Compilation;
using Mug.Typing;
using System.Collections.Generic;
using System.Text;

namespace Mug.Syntax.AST
{
    public class BlockNode : INode
    {
        internal static string Indent = "";

        public string NodeName => "Block";

        public List<INode> Statements { get; }

        public ModulePosition Position { get; set; }

        public BlockNode(List<INode> statements = null)
        {
            Statements = statements ?? new();
        }

        public override string ToString()
        {
            Indent += "  ";
            var result = new StringBuilder($"{{\n");

            foreach (var statement in Statements)
                result.AppendLine($"{Indent}{statement}");

            Indent = Indent[..^2];
            result.Append($"{Indent}}}");
            return result.ToString();
        }
    }
}
