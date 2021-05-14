using Nylon.Compilation;
using Nylon.Models.Lexer;
using System.Collections.Generic;

namespace Nylon.Models.Parser.AST.Statements
{
    public class TypeStatement : INode
    {
        public string NodeKind => "Struct";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<Token> Generics { get; set; } = new();
        public List<FieldNode> BodyFields { get; set; } = new();
        public List<FunctionStatement> BodyMethods { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
    }
}
