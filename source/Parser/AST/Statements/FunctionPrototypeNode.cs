using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public class FunctionPrototypeNode : INode
    {
        public string NodeKind => "FunctionPrototype";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public DataType Type { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
