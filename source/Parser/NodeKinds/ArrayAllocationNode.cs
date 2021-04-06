using Mug.Compilation;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds
{
    public class ArrayAllocationNode : INode
    {
        public string NodeKind => "ArrayAllocationNode";
        public MugType Type { get; set; }
        public INode Size { get; set; }
        public bool SizeIsImplicit { get; set; }
        private List<INode> _body { get; set; } = new();
        public INode[] Body
        {
            get
            {
                return _body.ToArray();
            }
        }
        public ModulePosition Position { get; set; }

        public void AddArrayElement(INode element)
        {
            _body.Add(element);
        }
    }
}
