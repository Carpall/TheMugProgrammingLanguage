using Zap.Compilation;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using System.Collections.Generic;

namespace Zap.Models.Parser.AST.Statements
{
    public class FunctionStatement : INode
    {
        public string NodeKind => "Function";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public ZapType ReturnType { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public BlockNode Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
