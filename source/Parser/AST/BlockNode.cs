using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
    public class BlockNode : INode
    {
        public string NodeKind => "Block";
        public readonly List<INode> Statements = new();
        public ModulePosition Position { get; set; }
    }
}
