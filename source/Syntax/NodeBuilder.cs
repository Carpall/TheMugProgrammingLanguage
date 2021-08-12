using Mug.Compilation;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Mug.Syntax.AST;
using Mug.Typing;

namespace Mug.Syntax
{
    public class NodeBuilder : INode, ICollection<INode>
    {
        public string NodeName => "NodeBuilder";
        private readonly List<INode> _nodes = new();
        public ModulePosition Position { get; set; }

        

        public int Count => _nodes.Count;
        public bool IsReadOnly => false;
        public INode this[int index] { get => _nodes[index]; set => _nodes[index] = value; }

        public void Add(INode item)
        {
            _nodes.Add(item);
        }

        public void Clear()
        {
            _nodes.Clear();
        }

        public bool Contains(INode item)
        {
            return _nodes.Contains(item);
        }

        public void CopyTo(INode[] array, int arrayIndex)
        {
            _nodes.CopyTo(array, arrayIndex);
        }

        public bool Remove(INode item)
        {
            return _nodes.Remove(item);
        }

        public IEnumerator<INode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        public void AddRange(NodeBuilder members)
        {
            _nodes.AddRange(members);
        }

        public void Prepend(INode node)
        {
            _nodes.Insert(0, node);
        }
    }
}
