using Mug.Compilation;
using System;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds
{
    public class BlockNode : INode
    {
        public string NodeKind => "Block";
        public INode[] Statements
        {
            get
            {
                return statements.ToArray();
            }
        }

        public ModulePosition Position { get; set; }

        private readonly List<INode> statements = new();
        public void Add(INode node)
        {
            statements.Add(node);
        }
    }
}
