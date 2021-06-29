using Mug.Compilation;
using Mug.TypeSystem;
using System;

namespace Mug.Syntax.AST
{
    public class VariableNode : INode
    {
        public string NodeName => "Var";
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
