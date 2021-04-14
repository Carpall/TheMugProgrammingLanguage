using Mug.Compilation;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds.Statements
{
    public class CallStatement : INode
    {
        public string NodeKind => "Call";
        public NodeBuilder Parameters { get; set; } = new();
        public INode Name { get; set; }
        public List<MugType> Generics { get; set; } = new();
        public bool IsBuiltIn { get; set; }
        public ModulePosition Position { get; set; }
    }
}
