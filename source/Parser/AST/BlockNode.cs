using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Parser.AST
{
    public class BlockNode : INode
    {
        public string NodeName => "Block";
        public readonly List<INode> Statements = new();
        public ModulePosition Position { get; set; }
    }
}
