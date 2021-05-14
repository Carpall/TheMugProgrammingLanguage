using Nylon.Compilation;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST
{
    public class BlockNode : INode
    {
        public string NodeKind => "Block";
        public readonly List<INode> Statements = new();
        public ModulePosition Position { get; set; }
    }
}
