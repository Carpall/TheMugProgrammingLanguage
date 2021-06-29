using Mug.Compilation;
using Mug.TypeSystem;
using System;

namespace Mug.Syntax.AST
{
    public struct ConstantNode : INode
    {
        public string NodeName => "Const";
        public String Name { get; set; }
        public DataType Type { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
