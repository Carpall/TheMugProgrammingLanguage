using Nylon.Compilation;
using Nylon.Models.Lexer;

namespace Nylon.Models.Parser.AST
{
    public struct TryExpressionNode : INode
    {
        public string NodeKind => "Try";
        public INode Expression { get; set; }
        public ModulePosition Position { get; set; }
    }
}