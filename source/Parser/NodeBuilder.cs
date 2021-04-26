using Mug.Compilation;
using System.Collections;
using System.Collections.Generic;

namespace Mug.Models.Parser
{
    public class NodeBuilder : INode, ICollection<INode>
    {
        public string NodeKind => "NodeBuilder";
        public List<INode> Nodes = new();
        public ModulePosition Position { get; set; }

        public int Count => throw new System.NotImplementedException();

        public bool IsReadOnly => throw new System.NotImplementedException();

        public void Add(INode item)
        {
            Nodes.Add(item);
        }

        public void Clear()
        {
            Nodes.Clear();
        }

        public bool Contains(INode item)
        {
            return Nodes.Contains(item);
        }

        public void CopyTo(INode[] array, int arrayIndex)
        {
            Nodes.CopyTo(array, arrayIndex);
        }

        public bool Remove(INode item)
        {
            return Nodes.Remove(item);
        }

        public IEnumerator<INode> GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }
    }
}
