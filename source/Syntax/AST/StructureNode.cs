using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Mug.Syntax.AST
{
    public class StructureNode : INode
    {
        public string NodeName => "Struct";
        public List<FieldNode> BodyFields { get; set; } = new();
        public List<VariableNode> BodyMembers { get; set; } = new();
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            var block = new BlockNode();
            block.Statements.AddRange(BodyFields);
            block.Statements.AddRange(BodyMembers);

            return $"(struct {block})";
        }
    }
}
