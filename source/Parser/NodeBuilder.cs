using Mug.Compilation;
using System.Collections.Generic;

namespace Mug.Models.Parser
{
  public class NodeBuilder : INode
    {
        public string NodeKind => "NodeBuilder";
        public List<INode> Nodes = new();
        public ModulePosition Position { get; set; }
    }
}
