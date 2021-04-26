using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
{
  public class CastExpressionNode : INode
    {
        public string NodeKind => "Cast";
        public INode Expression { get; set; }
        public IType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}