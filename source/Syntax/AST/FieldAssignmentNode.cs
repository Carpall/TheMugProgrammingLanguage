using Mug.Compilation;
using Mug.Typing;
using System;

namespace Mug.Syntax.AST
{
    public class FieldAssignmentNode : INode
    {
        public string NodeName => "FieldAssignment";
        public string Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            return $"{Name}: {Body}";
        }
    }
}
