using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class BlockNode : INode
    {
        public string NodeName => "Block";
        public readonly List<INode> Statements = new();
        public ModulePosition Position { get; set; }
    }
}
