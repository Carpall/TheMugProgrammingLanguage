using Zap.Compilation;
using Zap.TypeSystem;

namespace Zap.Models.Parser.AST
{
    public class FieldNode : INode
    {
        public string NodeKind => "Field";
        public string Name { get; set; }
        public ZapType Type { get; set; }
        public ModulePosition Position { get; set; }
    }
}
