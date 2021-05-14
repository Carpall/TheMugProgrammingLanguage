using Nylon.Compilation;
using Nylon.TypeSystem;
using System;

namespace Nylon.Models.Parser.AST.Statements
{
    public class VariableStatement : INode
    {
        public string NodeKind => "Var";
        public string Name { get; set; }
        public DataType Type { get; set; }
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
