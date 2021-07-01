using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public struct CaseNode : INode
    {
        public string NodeName => "SwitchNode";
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

    public class SwitchCaseNode : INode
    {
        public string NodeName => "Switch";
        public INode Expression { get; set; }
        public List<CaseNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
