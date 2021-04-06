using Mug.Compilation;
using Mug.TypeSystem;
using System;

namespace Mug.Models.Parser.NodeKinds
{
    public class CastExpressionNode : INode
    {
        public string NodeKind => "Cast";
        public INode Expression { get; set; }
        public MugType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}