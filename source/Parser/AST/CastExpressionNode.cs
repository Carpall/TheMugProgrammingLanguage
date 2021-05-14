using Nylon.Compilation;
using Nylon.TypeSystem;

namespace Nylon.Models.Parser.AST
{
    public class CastExpressionNode : INode
    {
        public string NodeKind => "Cast";
        public INode Expression { get; set; }
        public DataType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}