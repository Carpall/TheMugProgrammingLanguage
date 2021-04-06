using Mug.Compilation;
using System;
using System.Collections.Generic;

namespace Mug.Models.Parser
{
    public class NodeBuilder : INode
    {
        public string NodeKind => "NodeBuilder";
        private readonly List<INode> nodes = new();
        public INode[] Nodes
        {
            get
            {
                return nodes.ToArray();
            }
        }
        public ModulePosition Position { get; set; }
        public int Length
        {
            get
            {
                return nodes.Count;
            }
        }

        public void Add(INode node)
        {
            nodes.Add(node);
        }

        public void Insert(int index, INode e)
        {
            nodes.Insert(index, e);
        }
    }
}
