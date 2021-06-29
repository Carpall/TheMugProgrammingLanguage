using Mug.Compilation;
using System;

namespace Mug.Syntax.AST
{
    public class FieldAssignmentNode : INode
    {
        public string NodeName => "FieldAssignment";
        public String Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
