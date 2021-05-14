using Nylon.Compilation;
using Nylon.TypeSystem;
using System;

namespace Nylon.Models.Parser.AST.Statements
{
    public struct ConstantStatement : INode
    {
        public string NodeKind => "Const";
        public String Name { get; set; }
        public DataType Type { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
