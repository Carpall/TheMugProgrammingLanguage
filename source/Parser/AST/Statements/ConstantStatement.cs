using Zap.Compilation;
using Zap.TypeSystem;
using System;

namespace Zap.Models.Parser.AST.Statements
{
    public struct ConstantStatement : INode
    {
        public string NodeKind => "Const";
        public String Name { get; set; }
        public ZapType Type { get; set; }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
