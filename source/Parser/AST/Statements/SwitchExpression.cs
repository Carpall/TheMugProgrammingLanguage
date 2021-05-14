using Nylon.Compilation;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public struct SwitchNode : INode
    {
        public string NodeKind => "SwitchNode";
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public bool IsElseNode
        {
            get
            {
                return Expression is null;
            }
        }
        public ModulePosition Position { get; set; }
    }

    public class SwitchExpression : INode
    {
        public string NodeKind => "Switch";
        public INode Expression { get; set; }
        public List<SwitchNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
