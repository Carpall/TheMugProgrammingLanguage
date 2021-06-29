using Mug.Compilation;

using System;

namespace Mug.Syntax.AST
{
    public class VariableNode : INode
    {
        public string NodeName => "Var";
        
        public string Name { get; set; }

        public INode Type { get; set; }

        public INode Body { get; set; }

        public ModulePosition Position { get; set; }

        public bool IsConst { get; set; }
        public bool IsMutable { get; set; }

        public bool IsAssigned
        {
            get
            {
                return Body is not BadNode;
            }
        }
    }
}
