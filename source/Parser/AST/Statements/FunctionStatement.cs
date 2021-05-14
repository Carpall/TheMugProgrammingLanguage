using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public class FunctionStatement : INode
    {
        public string NodeKind => "Function";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public DataType ReturnType { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public BlockNode Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
