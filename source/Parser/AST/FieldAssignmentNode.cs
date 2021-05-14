using Nylon.Compilation;
using System;

namespace Nylon.Models.Parser.AST
{
    public class FieldAssignmentNode : IStatement
    {
        public string NodeKind => "FieldAssignment";
        public String Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
