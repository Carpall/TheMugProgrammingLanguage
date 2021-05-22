using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST
{
    public class CastExpressionNode : INode
    {
        public string NodeName => "Cast";
        public INode Expression { get; set; }
        public DataType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}