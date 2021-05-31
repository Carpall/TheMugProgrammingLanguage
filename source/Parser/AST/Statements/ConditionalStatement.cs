using Mug.Compilation;
using Mug.Lexer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Parser.AST.Statements
{
    public class ConditionalStatement : INode
    {
        public string NodeName => "Condition";
        public TokenKind Kind { get; set; }
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }
        public ConditionalStatement ElseNode { get; set; }
    }
}
