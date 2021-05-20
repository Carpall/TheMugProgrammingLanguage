using Mug.Compilation;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST.Statements
{
    public class CallStatement : INode
    {
        public string NodeKind => "Call";
        public NodeBuilder Parameters { get; set; } = new();
        public INode Name { get; set; }
        public List<DataType> Generics { get; set; } = new();
        public bool IsBuiltIn { get; set; }
        public ModulePosition Position { get; set; }
    }
}
