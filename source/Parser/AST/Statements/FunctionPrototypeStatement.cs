/*using Mug.Compilation;
using Mug.Lexer;
using Mug.Symbols;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Parser.AST.Statements
{
    public class FunctionPrototypeStatement : INode
    {
        public string NodeName => "FunctionPrototype";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public DataType ReturnType { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
*/