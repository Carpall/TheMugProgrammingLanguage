using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds.Statements
{
  public struct MatchNode : INode
    {
        public string NodeKind => "MatchNode";
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

    public class MatchExpression : INode
    {
        public string NodeKind => "Match";
        public INode Expression { get; set; }
        public List<MatchNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
