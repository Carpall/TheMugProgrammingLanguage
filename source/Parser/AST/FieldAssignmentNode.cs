using Zap.Compilation;
using System;

namespace Zap.Models.Parser.AST
{
    public class FieldAssignmentNode : IStatement
    {
        public string NodeKind => "FieldAssignment";
        public String Name { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
