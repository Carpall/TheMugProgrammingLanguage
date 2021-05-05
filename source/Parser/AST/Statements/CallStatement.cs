using Zap.Compilation;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST.Statements
{
    public class CallStatement : INode
    {
        public string NodeKind => "Call";
        public NodeBuilder Parameters { get; set; } = new();
        public INode Name { get; set; }
        public List<ZapType> Generics { get; set; } = new();
        public bool IsBuiltIn { get; set; }
        public ModulePosition Position { get; set; }
    }
}
