using Zap.Compilation;
using Zap.TypeSystem;
using System;

namespace Zap.Models.Parser.AST.Statements
{
    public class VariableStatement : INode
    {
        public string NodeKind => "Var";
        public string Name { get; set; }
        public ZapType Type { get; set; }
        public bool IsAssigned
        {
            get
            {
                return Body is not BadNode;
            }
        }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsConst { get; set; }
    }
}
