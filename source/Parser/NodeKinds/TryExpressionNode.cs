using Mug.Compilation;
using Mug.Models.Lexer;

namespace Mug.Models.Parser.NodeKinds
{
    public struct TryExpressionNode : INode
    {
        public string NodeKind => "Try";
        public INode Expression { get; set; }
        public ModulePosition Position { get; set; }
    }
}